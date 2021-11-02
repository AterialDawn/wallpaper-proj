using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using player.Core;
using player.Core.Audio;
using player.Core.Input;
using player.Core.Render;
using player.Utility;
using player.Core.Service;
using Log = player.Core.Logging.Logger;
using player.Core.Settings;

namespace player.Core.Commands
{
    class PlayerCommands : IService
    {
        public string ServiceName { get { return "PlayerCommands"; } }

        internal PlayerCommands() { }

        public void Initialize()
        {
            //Register all commands and their handlers
            ConsoleManager conMan = ServiceManager.GetService<ConsoleManager>();
            
            conMan.RegisterCommandHandler("quit", quitHandler);
            conMan.RegisterCommandHandler("fps", fpsHandler);
            conMan.RegisterCommandHandler("fpsmax", fpsMaxHandler);
            conMan.RegisterCommandHandler("dfps", dFpsHandler);
            conMan.RegisterCommandHandler("smooth", smoothingHandler);
            if (!VisGameWindow.WasapiMode)
            {
                conMan.RegisterCommandHandler("load", loadSongHandler);
                conMan.RegisterCommandHandler("open", loadSongHandler);
                conMan.RegisterCommandHandler("play", togglePlaybackHandler);
                conMan.RegisterCommandHandler("pause", togglePlaybackHandler);
                conMan.RegisterCommandHandler("volume", volumeHandler);
                conMan.RegisterCommandHandler("vol", volumeHandler);
            }
            else
            {
                conMan.RegisterCommandHandler("wasapilist", listWasapiDevicesHandler);
                conMan.RegisterCommandHandler("wasapiset", setWasapiDeviceHandler);
            }
        }

        public void Cleanup()
        {

        }

        private void smoothingHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length < 1)
            {
                args.ForwardLineToThisHandler = true;
                Log.Log("Enter a smoothing factor [0-1] : ");
                return;
            }
            
            if (args.Arguments[0].Equals("q", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            float smoothingVal = 0f;
            if (!float.TryParse(args.Arguments[0], out smoothingVal))
            {
                args.ForwardLineToThisHandler = true;
                Log.Log("Enter a smoothing factor [0-1] : ");
                return;
            }
            ServiceManager.GetService<SettingsService>().SetSetting(SettingsKeys.SDP_Smoothing, smoothingVal);
            Log.Log("Set smoothing factor to {0}", smoothingVal);
        }

        private void setWasapiDeviceHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length < 1)
            {
                args.ForwardLineToThisHandler = true;
                Log.Log("Enter a wasapi device index : ");
                return;
            }

            if (args.Arguments[0].Equals("q", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            int index = 0;
            if (!int.TryParse(args.Arguments[0], out index))
            {
                args.ForwardLineToThisHandler = true;
                Log.Log("Enter a wasapi device index : ");
                return;
            }

            if (ServiceManager.GetService<WasapiSoundManager>().SetDeviceIndex(index))
            {
                Log.Log("Successfully set device index to {0}!", index);
            }
            else
            {
                Log.Log("Unable to initialize wasapi index {0}!", index);
            }
        }

        private void listWasapiDevicesHandler(object sender, ConsoleLineReadEventArgs args)
        {
            ServiceManager.GetService<WasapiSoundManager>().PrintDevicesToConsole();
        }

        private void loadSongHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length == 0)
            {
                Log.Log("No path specified");
                return;
            }
            string file = args.Arguments[0];
            string lLine = file.ToLowerInvariant();
            if (lLine == "q" || lLine == "exit")
            {
                Log.Log("No path specified");
                return;
            }

            if (!UtilityMethods.DoesFileExist(file))
            {
                Log.Log("Path does not exist. Please enter the path to an MP3|FLAC file ");
                args.ForwardLineToThisHandler = true;
                return;
            }
            else if (Path.GetExtension(file).ToLower() == ".mp3" || Path.GetExtension(file).ToLower() == ".flac")
            {
                if (ServiceManager.GetService<BassSoundManager>().GetStreamActive() != Un4seen.Bass.BASSActive.BASS_ACTIVE_STOPPED)
                {
                    ServiceManager.GetService<BassSoundManager>().StopStream();
                }
                ServiceManager.GetService<BassSoundManager>().SetCurrentSong(file);
                Log.Log("Loaded song " + file);
                return;
            }
            else
            {
                Log.Log("This is not a valid file. Please enter the path to a valid file [q to abort] ");
                args.ForwardLineToThisHandler = true;
                return;
            }
        }

        private void volumeHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length == 0)
            {
                Log.Log("No volume specified.");
                return;
            }
            string vol = args.Arguments[0];
            string lLine = vol.ToLowerInvariant();
            if (lLine == "q" || lLine == "quit")
            {
                Log.Log("No volume specified");
                return;
            }
            if (string.IsNullOrWhiteSpace(vol))
            {
                Log.Log("Not a number. Please enter a number between 0-100 [q to abort]");
                args.ForwardLineToThisHandler = true;
                return;
            }

            int parsedInt = -1;
            if (!int.TryParse(vol, out parsedInt))
            {
                Log.Log("Not a number. Please enter a number between 0-100 [q to abort]");
                args.ForwardLineToThisHandler = true;
                return;
            }
            parsedInt = Math.Max(Math.Min(100, parsedInt), 0);
            ServiceManager.GetService<BassSoundManager>().SetVolume(parsedInt);
            Log.Log("Set volume to {0}", parsedInt);
        }

        private void quitHandler(object sender, ConsoleLineReadEventArgs args)
        {
            Log.Log("Exiting!");
            VisGameWindow.ThisForm.Close();
        }

        private void togglePlaybackHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Command == "play")
            {
                if (ServiceManager.GetService<BassSoundManager>().IsStreamPaused())
                {
                    Log.Log("Playing stream.");
                    ServiceManager.GetService<BassSoundManager>().PlayCurrentStream();
                }
                else
                {
                    Log.Log("Stream is already playing!");
                }
            }
            else if (args.Command == "pause")
            {
                //Pause generally acts as a toggle, so lets copy that functionality
                if (ServiceManager.GetService<BassSoundManager>().IsStreamPaused())
                {
                    ServiceManager.GetService<BassSoundManager>().PlayCurrentStream();
                    Log.Log("Playing stream.");
                }
                else
                {
                    ServiceManager.GetService<BassSoundManager>().PauseCurrentStream();
                    Log.Log("Paused stream.");
                }
            }
        }

        private void fpsHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length == 0)
            {
                Log.Log("No fps specified.");
                return;
            }
            string fps = args.Arguments[0].ToLowerInvariant();
            if (fps == "fast" || fps == "unlimited" || fps == "nolimit" || fps == "ultra")
            {
                VisGameWindow.ThisForm.FpsLimiter.Enabled = false;
                Log.Log("FPS unlocked");
                return;
            }
            int newFps = -1;
            if (!int.TryParse(fps, out newFps))
            {
                Log.Log("Cannot parse {0} as an integer", fps);
                return;
            }
            newFps = UtilityMethods.Clamp(newFps, 5, 999);
            FpsLimitHelper renderingLimiter = VisGameWindow.ThisForm.FpsLimiter;
            renderingLimiter.MinimumFps = renderingLimiter.IdleFps = newFps;
            if (renderingLimiter.MinimumFps >= renderingLimiter.MaximumFps) renderingLimiter.MaximumFps = newFps;
            if (renderingLimiter.MinimumFps < 60 && renderingLimiter.MaximumFps > 60) renderingLimiter.MaximumFps = 60;
            Log.Log("Changed FPS lock to {0}", newFps);
        }

        private void fpsMaxHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length == 0)
            {
                Log.Log("No fps specified.");
                return;
            }
            string fps = args.Arguments[0].ToLowerInvariant();
            int newFps = -1;
            if (!int.TryParse(fps, out newFps))
            {
                Log.Log("Cannot parse {0} as an integer", fps);
                return;
            }
            newFps = UtilityMethods.Clamp(newFps, 5, 999);
            FpsLimitHelper renderingLimiter = VisGameWindow.ThisForm.FpsLimiter;
            if (renderingLimiter.IdleFps > newFps || renderingLimiter.MinimumFps > newFps)
            {
                Log.Log($"New Max FPS is too low! Change idle/min fps first");
                return;
            }
            renderingLimiter.MaximumFps = newFps;
            Log.Log("Changed FPS lock to {0}", newFps);
        }

        private void dFpsHandler(object sender, ConsoleLineReadEventArgs args)
        {
            if (args.Arguments.Length == 0)
            {
                Log.Log("No DataUpdate fps specified.");
                return;
            }
            string fps = args.Arguments[0].ToLowerInvariant();
            int newFps = -1;
            if (!int.TryParse(fps, out newFps))
            {
                Log.Log("Cannot parse {0} as an integer", fps);
                return;
            }
            newFps = UtilityMethods.Clamp(newFps, 5, 999);
            var renderingLimiter = ServiceManager.GetService<SoundDataProcessor>().FpsLimiter;
            renderingLimiter.MinimumFps = renderingLimiter.IdleFps = newFps;
            if (renderingLimiter.MinimumFps >= renderingLimiter.MaximumFps) renderingLimiter.MaximumFps = newFps;
            if (renderingLimiter.MinimumFps < 60 && renderingLimiter.MaximumFps > 60) renderingLimiter.MaximumFps = 60;
            Log.Log("Changed DataUpdate FPS lock to {0}", newFps);
        }
    }
}
