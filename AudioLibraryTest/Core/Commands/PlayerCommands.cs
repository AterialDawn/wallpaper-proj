using player.Core.Audio;
using player.Core.Input;
using player.Core.Render;
using player.Core.Service;
using player.Core.Settings;
using player.Utility;
using System;
using Log = player.Core.Logging.Logger;

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
            if (VisGameWindow.WasapiMode)
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

        private void quitHandler(object sender, ConsoleLineReadEventArgs args)
        {
            Log.Log("Exiting!");
            VisGameWindow.ThisForm.Close();
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
