using FullSerializer;
using Nito.AsyncEx;
using OpenTK.Graphics;
using player.Core.Logging;
using player.Core.Render;
using player.Core.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace player.Core.Settings
{
    class WallpaperImageSettingsService : IService
    {
        public string ServiceName => "WallpaperImageSettings";
        string settingsFilePath = "";
        List<PathComponent> pathComponentList = new List<PathComponent>();
        SemaphoreSlim saveSemaphore = new SemaphoreSlim(1, 1);
        AsyncAutoResetEvent dirtyEvent = new AsyncAutoResetEvent();

        public void Initialize()
        {
            var settingsOverrideOpt = Program.CLIParser.ActiveOptions.Where(o => o.Item1.Equals("ImageSettingsFile")).FirstOrDefault();
            if (settingsOverrideOpt != null)
            {
                Logger.Log($"Overriding image settings file to {settingsOverrideOpt.Item2}");
                settingsFilePath = ServiceManager.GetService<SettingsService>().GetFileRelativeToSettings(settingsOverrideOpt.Item2 + ".json");
            }
            else
            {
                settingsFilePath = ServiceManager.GetService<SettingsService>().GetFileRelativeToSettings("ImageSettingsDB.json");
            }

            if (File.Exists(settingsFilePath))
            {
                try
                {
                    pathComponentList = JsonToObject<List<PathComponent>>(File.ReadAllText(settingsFilePath));
                }
                catch (Exception e)
                {
                    Logger.Log($"Unable to load ImageSettingsDB.json\n{e}");
                }
            }

            SaveOnTimer();
        }
        public void Cleanup()
        {
            bool taken = saveSemaphore.Wait(500); //wait up to 500ms to save settings when shutting down. maybe tweak?
            if (!taken) return;
            try
            {
                SaveSettings();
            }
            finally
            {
                saveSemaphore.Release();
            }
        }

        void SaveSettings()
        {
            string serialized = ObjectToJson(pathComponentList);

            try
            {
                File.WriteAllText(settingsFilePath, serialized);
            }
            catch (Exception e)
            {
                Logger.Log($"Error writing ImageSettingsDB.json\n{e}");
                try
                {
                    File.WriteAllText(settingsFilePath + ".lastchance", serialized);
                }
                catch (Exception e2)
                {
                    Logger.Log($"Also failed writing last chance.\n{e2}\n{serialized}"); //maybe file logging is turned on :)
                }
            }
        }

        private async void SaveOnTimer()
        {
            await Task.Yield();
            for (; ; )
            {
                await dirtyEvent.WaitAsync();
                //we were released, wait for 30 seconds to save the new config, but re-wait if the dirty event was set again
                for (; ; )
                {
                    var cancelTokenSource = new CancellationTokenSource(15 * 1000);
                    try
                    {
                        await dirtyEvent.WaitAsync(cancelTokenSource.Token);
                        //dirty event was set again, loop again and wait once more
                        //but wait a second or two to not check too much
                        await Task.Delay(1000);
                        continue;
                    }
                    catch (TaskCanceledException)
                    {
                        //timeout expired, proceed to save
                        break;
                    }
                }
                try
                {
                    bool taken = await saveSemaphore.WaitAsync(0);
                    if (!taken) continue;
                    try
                    {
                        SaveSettings();
                        ServiceManager.GetService<MessageCenterService>().ShowMessage("Image Settings saved!");
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Error saving settings on timer : {e}");
                    }
                    finally
                    {
                        saveSemaphore.Release();
                    }
                }
                catch (Exception e)
                {
                    Logger.Log($"SaveOnTimer : {e}");
                }
            }
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
            var pathComponents = new Uri(path).Segments.Where(s => s != "/").Select(s => s.Replace("/", "")).ToArray();
            var fileName = pathComponents[pathComponents.Length - 1];

            var parentComponentOfFile = traversePathComponents(pathComponents.Take(pathComponents.Length - 1), createIfNotExists);
            if (parentComponentOfFile == null) return null;

            var currentFile = parentComponentOfFile.SubComponents.Where(p => p.Name == pathComponents[pathComponents.Length - 1]).Cast<ImageFileComponent>().FirstOrDefault();
            if (currentFile == null && createIfNotExists)
            {
                currentFile = new ImageFileComponent(fileName);
                parentComponentOfFile.SubComponents.Add(currentFile);
            }

            if (createIfNotExists) //if we're creating something that might not exist, we probably intend to write a value to it.
            {
                dirtyEvent.Set();
            }
            return currentFile?.Settings;
        }

        public void ClearSettingsForPath(string path)
        {
            var pathComponents = new Uri(path).Segments.Where(s => s != "/").Select(s => s.Replace("/", "")).ToArray();
            var fileName = pathComponents[pathComponents.Length - 1];

            var parentComponentOfFile = traversePathComponents(pathComponents.Take(pathComponents.Length - 1), false);
            if (parentComponentOfFile == null) return;

            var currentFile = parentComponentOfFile.SubComponents.Where(p => p.Name == pathComponents[pathComponents.Length - 1]).Cast<ImageFileComponent>().FirstOrDefault();
            if (currentFile != null)
            {
                parentComponentOfFile.SubComponents.Remove(currentFile);
                Logger.Log($"Settings for image {fileName} removed");
            }
        }

        PathComponent traversePathComponents(IEnumerable<string> pathComponents, bool createIfNotExists)
        {
            PathComponent parentComponent = null;
            foreach (var component in pathComponents)
            {
                if (parentComponent == null)
                {
                    //start off from the root component
                    parentComponent = pathComponentList.Where(p => p.Name == component).FirstOrDefault();

                    if (parentComponent == null && createIfNotExists)
                    {
                        if (createIfNotExists)
                        {
                            parentComponent = new PathComponent(component);
                            pathComponentList.Add(parentComponent);
                        }
                        else
                        {
                            //dead end, cannot create
                            return null;
                        }
                    }
                }
                else
                {
                    var foundComponent = parentComponent.SubComponents.Where(p => p.Name == component).FirstOrDefault();

                    if (foundComponent == null)
                    {
                        if (createIfNotExists)
                        {
                            foundComponent = new PathComponent(component);

                            parentComponent.SubComponents.Add(foundComponent);
                        }
                        else
                        {

                            //dead end
                            return null;
                        }
                    }
                    parentComponent = foundComponent;
                }
            }

            return parentComponent;
        }

        internal static T JsonToObject<T>(string jsonString)
        {
            T obj = default(T);

            fsSerializer serializer = new fsSerializer();
            fsData data = fsJsonParser.Parse(jsonString);
            fsResult result = serializer.TryDeserialize<T>(data, ref obj);
            if (!result.Succeeded)
            {
                throw result.AsException;
            }
            return obj;
        }

        internal static string ObjectToJson(object objectToConvert)
        {
            fsSerializer serializer = new fsSerializer();
            serializer.AddProcessor(new ImageSettingsProcessor());
            fsData dataInst;
            fsResult result = serializer.TrySerialize(objectToConvert, out dataInst);
            if (!result.Succeeded)
            {
                throw result.AsException;
            }
            else
            {
                return fsJsonPrinter.CompressedJson(dataInst);
            }
        }



        class PathComponent
        {
            public string Name { get; set; }
            public List<PathComponent> SubComponents { get; set; } = new List<PathComponent>();

            public PathComponent() { }

            public PathComponent(string name)
            {
                Name = name;
            }
        }

        class ImageFileComponent : PathComponent
        {
            public ImageSettings Settings { get; set; } = new ImageSettings();
            public ImageFileComponent() { }

            public ImageFileComponent(string name)
            {
                Name = name;
            }
        }

    }

    class ImageSettingsProcessor : fsObjectProcessor
    {
        Dictionary<string, PropertyInfo> propInfoCache = new Dictionary<string, PropertyInfo>();

        public ImageSettingsProcessor()
        {
            foreach (var prop in typeof(ImageSettings).GetProperties())
            {
                propInfoCache.Add(prop.Name, prop);
            }
        }
        public override bool CanProcess(Type type)
        {
            return type == typeof(ImageSettings);
        }

        public override void OnAfterSerialize(Type storageType, object instance, ref fsData data)
        {
            var dict = data.AsDictionary;
            var reference = new ImageSettings();

            List<string> propsToRemove = new List<string>();

            foreach (var prop in dict)
            {
                var propInfo = propInfoCache[prop.Key];
                var serializedValue = propInfo.GetValue(instance);
                var referenceValue = propInfo.GetValue(reference);

                if (serializedValue.Equals(referenceValue))
                {
                    propsToRemove.Add(prop.Key);
                }
            }

            foreach (var prop in propsToRemove)
            {
                dict.Remove(prop);
            }

        }
    }

    class ImageSettings
    {
        public BackgroundMode Mode { get; set; } = BackgroundMode.BorderedDefault;
        public Vector4 BackgroundColor { get; set; } = Vector4.One;
        public BackgroundAnchorPosition AnchorPosition { get; set; } = BackgroundAnchorPosition.Center;
        public SolidBackgroundStyle BackgroundStyle { get; set; } = SolidBackgroundStyle.SolidColor;

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
