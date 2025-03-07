using FolderSelect;
using player.Core.Render;
using player.Core.Service;
using player.Core.Settings;
using player.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using Log = player.Core.Logging.Logger;

namespace player.Renderers.BarHelpers
{
    class BackgroundFactory
    {
        List<string> registeredFiles = new List<string>();
        List<string> sourcePaths = new List<string>();

        List<string> animatedExtensions = new List<string>(new string[] { ".gif" });
        List<string> staticExtensions = new List<string>(new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".jfif" });
        List<string> videoExtensions = new List<string>(new string[] { ".mp4", ".webm", ".avi", ".mkv", ".mov", ".wmv" });
        Random rng = new Random();
        int currentIndex = -1; //-1 to use first image on startup

        public bool IsRandom { get; private set; } = true;
        public bool SingleBackgroundMode { get; private set; } = false;

        private SettingsAccessor<List<string>> registeredPathsKey;

        public BackgroundFactory()
        {
            if (Program.DisableFFmpeg) videoExtensions.Clear();
        }

        public void Initialize()
        {
            LoadPathsFromSettings();
            RescanAllSources();
        }

        public void RemoveFile(string path)
        {
            if (registeredFiles.Contains(path))
            {
                var indexOf = registeredFiles.IndexOf(path);
                registeredFiles.Remove(path);
                if (indexOf <= currentIndex) currentIndex--; //if the deleted image was already presented (in history), decrement the currentIndex by 1 to make up for 1 less item
                ServiceManager.GetService<MessageCenterService>().ShowMessage($"Removed {path} from {indexOf}");
                Log.Log($"Removed {path} from rotation");
            }
        }

        public void AddFile(string path)
        {
            int maxScan = UtilityMethods.Clamp(registeredFiles.Count - 1 - currentIndex, 0, 100);
            int idx = rng.Next(0, maxScan);
            registeredFiles.Insert(currentIndex + idx, path);
            ServiceManager.GetService<MessageCenterService>().ShowMessage($"Added {path} at {idx}");
            Log.Log($"Added {path} to rotation at pos {idx}");

        }

        private void LoadPathsFromSettings()
        {
            registeredPathsKey = ServiceManager.GetService<SettingsService>().GetAccessor<List<string>>(SettingsKeys.BarRenderer_SourceFolders, null);

            var tempList = registeredPathsKey.Value;
            if (tempList != null && tempList.Count > 0)
            {
                sourcePaths = tempList; //by reference, so updating this should save it in settings
            }
            else
            {
                registeredPathsKey.Set(sourcePaths); //by reference, updating this should save in settings
                System.Windows.Forms.MessageBox.Show("There are no registered background paths! Please select a folder to use for backgrounds.");
                using (FolderSelectDialog dialog = new FolderSelectDialog() { Title = "Select Folders to use" })
                {
                    if (!dialog.ShowDialog()) throw new InvalidOperationException("No path selected! Cannot continue!");
                    sourcePaths.Add(dialog.FileName);
                }
            }

            foreach (var directory in sourcePaths)
            {
                string curDirCache = directory;
                FileSystemWatcher watcher = new FileSystemWatcher(directory);
                watcher.Created += (s, e) =>
                {
                    if (IsValidExtension(Path.GetExtension(e.FullPath).ToLower()) && !registeredFiles.Contains(e.FullPath)) //sometimes programs (FIREFOX) creates a file multiple times, check that it hasn't already been added to the list first before continuing
                    {
                        Log.Log($"Watcher at {curDirCache} detected file creation at {e.FullPath}");
                        AddFile(e.FullPath);
                    }
                };
                watcher.Deleted += (s, e) =>
                {
                    if (IsValidExtension(Path.GetExtension(e.FullPath).ToLower()) && registeredFiles.Contains(e.FullPath))
                    {
                        Log.Log($"Watcher at {curDirCache} detected file deleted {e.FullPath}");
                        RemoveFile(e.FullPath);
                    }
                };
                watcher.Renamed += (s, e) =>
                {
                    bool oldValid = IsValidExtension(Path.GetExtension(e.OldFullPath).ToLower());
                    bool newValid = IsValidExtension(Path.GetExtension(e.FullPath).ToLower());
                    if (oldValid || newValid) Log.Log($"Watcher at {curDirCache} detected file rename from {e.OldFullPath} to {e.FullPath}");

                    if (oldValid && registeredFiles.Contains(e.OldFullPath))
                    {
                        RemoveFile(e.OldFullPath);
                    }
                    if (newValid && !registeredFiles.Contains(e.FullPath))
                    {
                        AddFile(e.FullPath);
                    }
                };
                watcher.EnableRaisingEvents = true;
            }
        }

        public void SetRandom(bool random)
        {
            if (IsRandom == random) return;
            IsRandom = random;
            if (IsRandom) ShuffleFiles();
            else OrderAlphabetically();
        }

        public bool AddSourceFolder(string path)
        {
            if (!Directory.Exists(path)) return false;
            if (sourcePaths.Contains(path)) return false;

            //Go through all files in 'path' and add any paths that have an IBackground implementation into the list
            List<string> filesToAdd = new List<string>();

            foreach (string fullPath in Directory.EnumerateFiles(path))
            {
                string extension = Path.GetExtension(fullPath);

                if (IsValidExtension(extension.ToLowerInvariant()))
                {
                    filesToAdd.Add(fullPath);
                }
            }

            if (filesToAdd.Count > 0)
            {
                Log.Log("Adding {0} images to factory", filesToAdd.Count);

                sourcePaths.Add(path);
                registeredFiles.AddRange(filesToAdd);
                if (registeredFiles.Count == 1) SingleBackgroundMode = true;
                else SingleBackgroundMode = false;

            }
            return true;
        }

        public bool RemoveSourceFolder(string path)
        {
            if (!sourcePaths.Contains(path)) return false;
            string normalizedSource = UtilityMethods.NormalizePath(path);

            List<string> pathsToRemove = new List<string>();
            foreach (string currentPath in registeredFiles)
            {
                string normalizedDirectory = UtilityMethods.NormalizePath(Path.GetDirectoryName(currentPath));
                if (normalizedSource == normalizedDirectory)
                {
                    pathsToRemove.Add(currentPath);
                }
            }

            foreach (string currentRemoving in pathsToRemove)
            {
                registeredFiles.Remove(currentRemoving);
            }

            pathsToRemove.Clear();

            sourcePaths.Remove(path);
            return true;
        }

        public string[] GetAllSourceFolders()
        {
            return sourcePaths.ToArray();
        }

        public void RescanAllSources()
        {
            List<string> tempFiles = new List<string>();
            foreach (string currentPath in sourcePaths)
            {
                foreach (string fullPath in Directory.EnumerateFiles(currentPath))
                {
                    string extension = Path.GetExtension(fullPath);

                    if (IsValidExtension(extension.ToLowerInvariant()))
                    {
                        tempFiles.Add(fullPath);
                    }
                }
            }

            registeredFiles.Clear();
            registeredFiles.AddRange(tempFiles);

            if (registeredFiles.Count == 1) SingleBackgroundMode = true;
            else SingleBackgroundMode = false;

            Log.Log("Added {0} total backgorunds", tempFiles.Count);
        }

        public IBackground GetNextBackground()
        {
            return GetBackgroundInternal(false);
        }

        public IBackground GetLastBackground()
        {
            return GetBackgroundInternal(true);
        }

        public IBackground LoadFromPath(string pathToUse)
        {
            string pathExtension = Path.GetExtension(pathToUse).ToLowerInvariant();
            if (animatedExtensions.Contains(pathExtension))
            {
                return new AnimatedImageBackground(pathToUse);
            }
            else if (staticExtensions.Contains(pathExtension))
            {
                return new StaticImageBackground(pathToUse);
            }
            else if (videoExtensions.Contains(pathExtension))
            {
                return new VideoBackground(pathToUse);
            }
            return null;
        }

        private IBackground GetBackgroundInternal(bool previous)
        {
            string pathToUse = GetNextPathToUse(previous);
            return LoadFromPath(pathToUse);
        }

        private bool IsValidExtension(string extension)
        {
            return (animatedExtensions.Contains(extension) || staticExtensions.Contains(extension) || videoExtensions.Contains(extension));
        }

        private string GetNextPathToUse(bool previous)
        {
            EnsureInitialized();

            //Only shuffle when reaching the end of the list, not when looping back to the end from the start.
            //May change.
            currentIndex = previous ? currentIndex - 1 : currentIndex + 1;
            if (currentIndex >= registeredFiles.Count)
            {
                ShuffleFiles();
                currentIndex = 0;
            }
            else if (currentIndex < 0)
            {
                currentIndex = registeredFiles.Count - 1;
            }
            return registeredFiles[currentIndex];
        }

        private void ShuffleFiles()
        {
            if (!IsRandom) return;

            int n = registeredFiles.Count;
            while (n > 1)
            {
                int k = (rng.Next(0, n) % n);
                n--;
                string value = registeredFiles[k];
                registeredFiles[k] = registeredFiles[n];
                registeredFiles[n] = value;
            }

            Log.Log($"Sorted {registeredFiles.Count} wallpapers randomly!");
        }

        private void OrderAlphabetically()
        {
            registeredFiles.Sort((a, b) => string.Compare(a, b, true));
            Log.Log($"Sorted {registeredFiles.Count} wallpapers alphabetically!");
        }

        private void EnsureInitialized()
        {
            if (registeredFiles.Count == 0)
            {
                throw new InvalidOperationException("No background paths specified!");
            }

            if (currentIndex == -1) ShuffleFiles();
        }
    }
}
