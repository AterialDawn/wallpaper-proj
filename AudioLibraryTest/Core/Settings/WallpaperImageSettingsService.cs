using OpenTK.Graphics;
using player.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace player.Core.Settings
{
    class WallpaperImageSettingsService : IService
    {
        public string ServiceName => "WallpaperImageSettings";
        SettingsService settings;
        List<PathComponent> pathComponentList = new List<PathComponent>();

        public void Initialize()
        {
            settings = ServiceManager.GetService<SettingsService>();

            var loaded = settings.GetSettingAs<List<PathComponent>>("Wallpaper.ImageSettings", null);
            if (loaded != null)
            {
                pathComponentList = loaded;
            }

            settings.SetSetting("Wallpaper.ImageSettings", pathComponentList);
        }
        public void Cleanup()
        {
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
            return currentFile?.Settings;
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

    class ImageSettings
    {
        public BackgroundMode Mode { get; set; } = BackgroundMode.BorderedDefault;
        public Vector4 BackgroundColor { get; set; } = Vector4.One;
        public BackgroundAnchorPosition AnchorPosition { get; set; } = BackgroundAnchorPosition.Center;

        public int TrimPixelsLeft { get; set; } = 0;
        public int TrimPixelsRight { get; set; } = 0;
        public int TrimPixelsTop { get; set; } = 0;
        public int TrimPixelsBottom { get; set; } = 0;
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
}
