using LiteDB;
using player.Core.Logging;
using player.Core.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace player.Core.Settings
{
    class WallpaperImageSettingsService : IService
    {
        readonly TimeSpan UpdateSettingsAfter = TimeSpan.FromSeconds(5);

        public bool WereSettingsUpdatedThisFrame { get { return lastFrameSettingsChanged == TimeManager.FrameNumber; } }

        public string ServiceName => "WallpaperImageSettings";
        string settingsFilePath = "";
        LiteDatabase db;
        ILiteCollection<ImageSettings> collection;
        long lastFrameSettingsChanged = 0;
        Dictionary<string, ImageSettingsContainer> potentiallyDirtySettings = new Dictionary<string, ImageSettingsContainer>();

        public void Initialize()
        {
            var settingsOverrideOpt = Program.CLIParser.ActiveOptions.Where(o => o.Item1.Equals("ImageSettingsFile")).FirstOrDefault();
            if (settingsOverrideOpt != null)
            {
                Logger.Log($"Overriding image settings file to {settingsOverrideOpt.Item2}");
                settingsFilePath = ServiceManager.GetService<SettingsService>().GetFileRelativeToSettings(settingsOverrideOpt.Item2 + ".db");
            }
            else
            {
                settingsFilePath = ServiceManager.GetService<SettingsService>().GetFileRelativeToSettings("ImageSettings.db");
            }

            InitCustomMappers();

            db = new LiteDatabase(settingsFilePath);
            collection = db.GetCollection<ImageSettings>("imageSettings");
            collection.EnsureIndex(i => i.FilePath);
            VisGameWindow.OnAfterThreadedRender += VisGameWindow_OnAfterThreadedRender;
        }

        private void VisGameWindow_OnAfterThreadedRender(object sender, EventArgs e)
        {
            var toDelete = new List<string>();
            if (potentiallyDirtySettings.Count > 0) //if any settings were updated, commit the change to the db after the timeout expires
            {
                foreach (var kvp in potentiallyDirtySettings)
                {
                    if ((DateTime.Now - kvp.Value.TimeWasDirty) > UpdateSettingsAfter)
                    {
                        collection.Update(kvp.Value.Settings);
                        toDelete.Add(kvp.Key);
                        Logger.Log($"Committing changes to {kvp.Value.Settings.FilePath}");
                    }
                }
            }
            foreach (var delete in toDelete)
            {
                potentiallyDirtySettings.Remove(delete);
            }
        }

        public void Cleanup()
        {
            foreach (var kvp in potentiallyDirtySettings)
            {
                collection.Update(kvp.Value.Settings);
            }
            db.Dispose();
        }

        /// <summary>
        /// gets the ImageSettings for the specified path. Creates a new ImageSettings if <paramref name="createIfNotExists"/> is true.
        /// Returns null if not found and <paramref name="createIfNotExists"/> was false
        /// </summary>
        /// <param name="path"></param>
        /// <param name="createIfNotExists"></param>
        /// <returns></returns>
        public ImageSettings GetImageSettingsForPath(string path, bool createIfNotExists = false)
        {
            string normalizedPath = NormalizePath(path);
            if (potentiallyDirtySettings.ContainsKey(normalizedPath)) //return the same object obtained this frame already
            {
                if (createIfNotExists)
                {
                    var container = potentiallyDirtySettings[normalizedPath];
                    container.TimeWasDirty = DateTime.Now;
                    return container.Settings;
                }
                else
                {
                    return potentiallyDirtySettings[normalizedPath].Settings;
                }
            }
            var imageSetting = collection.FindOne(i => i.FilePath == normalizedPath);

            if (imageSetting == null && createIfNotExists)
            {
                imageSetting = new ImageSettings { FilePath = normalizedPath, Id = ObjectId.NewObjectId() };
                collection.Insert(imageSetting);
                Logger.Log($"Created new image settings for {Path.GetFileName(normalizedPath)}");
            }
            if (createIfNotExists)
            {
                lastFrameSettingsChanged = TimeManager.FrameNumber;
                potentiallyDirtySettings.Add(normalizedPath, new ImageSettingsContainer { TimeWasDirty = DateTime.Now, Settings = imageSetting });
            }
            return imageSetting;
        }

        public void ClearSettingsForPath(string path)
        {
            var normalizedPath = NormalizePath(path);
            if (potentiallyDirtySettings.ContainsKey(normalizedPath))
            {
                potentiallyDirtySettings.Remove(normalizedPath);
            }
            var imageSetting = collection.FindOne(i => i.FilePath == normalizedPath);
            if (imageSetting != null)
            {
                collection.Delete(imageSetting.Id);
                Logger.Log($"Deleted image settings for {Path.GetFileName(normalizedPath)}");
            }
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private void InitCustomMappers()
        {
            BsonMapper.Global.RegisterType<Vector4>(
                serialize: (vec4) => new BsonArray(new[] { new BsonValue(vec4.X), new BsonValue(vec4.Y), new BsonValue(vec4.Z), new BsonValue(vec4.W) }),
                deserialize: (bson) => new Vector4((float)bson.AsArray[0].AsDouble, (float)bson.AsArray[1].AsDouble, (float)bson.AsArray[2].AsDouble, (float)bson.AsArray[3].AsDouble)
                );
        }
    }

    class ImageSettingsContainer
    {
        public ImageSettings Settings { get; set; }
        public DateTime TimeWasDirty { get; set; } = DateTime.Now;
    }

    class ImageSettings
    {
        public ObjectId Id { get; set; }
        public string FilePath { get; set; }
        public BackgroundMode Mode { get; set; } = BackgroundMode.BorderedDefault;
        public Vector4 BackgroundColor { get; set; } = Vector4.One;
        public BackgroundAnchorPosition AnchorPosition { get; set; } = BackgroundAnchorPosition.Center;
        public SolidBackgroundStyle BackgroundStyle { get; set; } = SolidBackgroundStyle.StretchEdge;

        //image is cropped via GDI+ with these variables
        public int TrimPixelsLeft { get; set; } = 0;
        public int TrimPixelsRight { get; set; } = 0;
        public int TrimPixelsTop { get; set; } = 0;
        public int TrimPixelsBottom { get; set; } = 0;
        //image is cropped via opengl with these
        public int RenderTrimLeft { get; set; } = 0;
        public int RenderTrimRight { get; set; } = 0;
        public int RenderTrimTop { get; set; } = 0;
        public int RenderTrimBot { get; set; } = 0;
        public int SrcSampleTop { get; set; } = 0;
        public int SrcSampleLeft { get; set; } = 0;
        public int SrcSampleBot { get; set; } = 10;
        public int SrcSampleRight { get; set; } = 10;
        public int StretchXPos { get; set; } = 0;
        public int StretchWidth { get; set; } = 10;
        public bool EditingDisabled { get; set; } = false;
    }

    enum BackgroundMode
    {
        BorderedDefault = 0,
        SolidBackground = 1
    }

    enum BackgroundAnchorPosition
    {
        Center = 0,
        Left = 1,
        Right = 2
    }

    enum SolidBackgroundStyle
    {
        SolidColor = 0,
        SourceCopyMirror = 1,
        SourceCopyStretch = 2,
        StretchEdge = 3
    }
}
