using Aterial.Utility;
using player.Core;
using player.Core.Audio;
using player.Core.Settings;
using player.Utility;
using System;
using System.IO;
using System.Reflection;
using Un4seen.Bass;
using Log = player.Core.Logging.Logger;

namespace player
{
    class Program
    {
        public static bool DisableFFmpeg = false;
        public static bool DisableAudioProcessing = false;
        public static CommandLineParser CLIParser;
        public static string VersionNumber;
        static bool EnableKillingShadowplay = false;

        static int width = 1280, height = 720;
        [STAThread]
        static void Main(string[] args)
        {
            if (CheckIfShouldRelaunch(args)) return;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Log.InitLogging(Path.Combine(Environment.CurrentDirectory, "Logs"));
            Log.LogToConsoleEnabled = true;
            Log.LogToFileEnabled = false;

            VersionNumber = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            CLIParser = new CommandLineParser();
            CLIParser.RegisterUsageCallbacks();
            CLIParser.AddOption("DisableRTT", "Disables Render to Texture", false, DisableRTTOption);
            CLIParser.AddOption("Resolution", "Changes the starting resolution (WxH)", true, ChangeResOption);
            CLIParser.AddOption("Wallpaper", "Enables wallpaper mode", false, WallpaperOptionCallback);
            CLIParser.AddOption("WallpaperPos", "Wallpaper position, ex: -WallpaperPos 0x0", true);
            CLIParser.AddOption("WallpaperSize", "Wallpaper mode size, ex -WallpaperSize 1920x1080", true);
            CLIParser.AddOption("PlayerMode", "Enables the visualizer's WASAPI mode (Vista+)", false, WasapiOptionCallback);
            CLIParser.AddOption("WasapiDevice", "Sets the default Wasapi device index to this", true, WasapiDeviceIndexCallback);
            CLIParser.AddOption("DisableFFmpeg", "Disables the loading of internal FFmpeg libraries. Video playback support disabled", false, DisableFFMmpegCallback);
            CLIParser.AddOption("WallpaperMonitorIndex", "Sets the monitor to attach to in wallpaper mode", true);
            CLIParser.AddOption("Font", "Sets the font for the text renderer. Specify as -Font \"Font Name,FontSize\"", true);
            CLIParser.AddOption("LogToFile", "Enables file logging", false, EnableFileLoggingCallback);
            CLIParser.AddOption("SettingsFile", "Changes the settings file used, without the extension", true, settingsFileCallback);
            CLIParser.AddOption("NoAudioProcess", "Disables all audio processing", false, (arg) => { DisableAudioProcessing = true; Log.Log("Audio Processing Disabled"); });
            CLIParser.AddOption("EnableShadowplayKill", "When starting in wallpaper mode, kills shadowplay as the app comes up to prevent it from capturing this", false, (_) => { EnableKillingShadowplay = true; Log.Log("Shadowplay Killer enabled"); });
            CLIParser.AddOption("ImageSettingsFile", "Override the filename for the ImageSettingsDB.json file", true);
            CLIParser.AddOption("GLDebug", "Enable OpenGL debug logging", false);
            CLIParser.IgnoreCase = true;
            CLIParser.ExitOnUsagePrinted = true;
            CLIParser.Parse(args);

            if (EnableKillingShadowplay)
            {
                //https://www.reddit.com/r/nvidia/comments/89mtzr/nvidia_freestyleoverlay_any_way_to_disable_for/
                //set 0x809D5F60 to 0x10000000 on profile for this app
                //figured out how to disable shadowplay per program
                ShadowplayUtil.KillIfNeeded();
            }

            //Initialize bass stuff
            BassNet.OmitCheckVersion = true;
            BassNet.Registration("trial@trial.com", "2X1837515183722");

            Log.Log("Creating rendering window...");
            try
            {
                using (VisGameWindow GameWindow = new VisGameWindow(width, height))
                {
                    GameWindow.VSync = OpenTK.VSyncMode.Off;
                    GameWindow.Run(0, 0);
                }

                Core.Service.ServiceManager.GetService<SettingsService>().Save();
            }
            catch (Exception e)
            {
                File.WriteAllText($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.fatal.txt", e.ToString());
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                File.WriteAllText($"{DateTime.Now:yyyy-dd-M--HH-mm-ss}.fatal.txt", e.ExceptionObject.ToString());
            }
        }

#pragma warning disable 0162
        private static bool CheckIfShouldRelaunch(string[] args)
        {
            return false; //disabled for now
            //if (System.Diagnostics.Debugger.IsAttached) return false; //Don't ever relaunch when debugger attached, since we'll lose debugger support
            foreach (var arg in args)
            {
                if (arg.Equals("-console", StringComparison.OrdinalIgnoreCase)) return false;
                else if (arg.Equals("-relaunched", StringComparison.OrdinalIgnoreCase)) return false;
            }

            ProcessCreator.ProcessCreator.RelaunchSelfWithoutConsole(args);
            return true;
        }
#pragma warning restore 0162

        private static void settingsFileCallback(string Arg)
        {
            SettingsService.SettingsFileName = Arg;
            Log.Log($"Using settings file {Arg}");
        }

        private static void EnableFileLoggingCallback(string Arg)
        {
            Log.LogToFileEnabled = true;
            Log.Log("File logging enabled");
        }

        private static void DisableFFMmpegCallback(string Arg)
        {
            Log.Log("FFmpeg loading disabled.");
            DisableFFmpeg = true;
        }

        private static void WasapiDeviceIndexCallback(string Arg)
        {
            int device = 0;
            if (int.TryParse(Arg, out device))
            {
                WasapiSoundManager.DefaultDevice = device;
                Log.Log("Setting default wasapi device to index {0}", device);
            }
            else
            {
                Log.Log("Only numeric indices are allowed.");
                Exit(-1);
            }
        }

        private static void WallpaperOptionCallback(string Arg)
        {
            if (Arg != null && Arg.ToLower().Equals("full"))
            {
                Log.Log("Wallpaper mode enabled (FullArea)");
                VisGameWindow.FormWallpaperMode = WallpaperMode.FullArea;
            }
            else
            {
                Log.Log("Wallpaper mode enabled (WorkingArea)");
                VisGameWindow.FormWallpaperMode = WallpaperMode.WorkingArea;
            }

        }

        private static void Exit(int exitCode = 0)
        {
            Environment.Exit(exitCode);
        }

        private static void ChangeResOption(string Arg)
        {
            if (!Arg.Contains("x"))
            {
                Log.Log("Invalid format, must be in (WxH) format");
                Exit(-1);
            }
            string[] SplitArg = Arg.Split('x');
            if (SplitArg.Length != 2)
            {
                Log.Log("Invalid format, must be in (WxH) format");
                Exit(-1);
            }

            int parsedWidth, parsedHeight;
            if (!int.TryParse(SplitArg[0], out parsedWidth))
            {
                Log.Log("Invalid format, must be in (WxH) format");
                Exit(-1);
            }
            if (!int.TryParse(SplitArg[1], out parsedHeight))
            {
                Log.Log("Invalid format, must be in (WxH) format");
                Exit(-1);
            }
            width = parsedWidth;
            height = parsedHeight;
            Log.Log("Changed default Width/Height to ({0}x{1})", width, height);
        }

        private static void DisableRTTOption(string Arg)
        {
            VisGameWindow.RTTEnabled = false;
            Log.Log("Disabled RTT (Render-To-Texture)");
        }

        private static void WasapiOptionCallback(string Arg)
        {
            VisGameWindow.WasapiMode = false;
            Log.Log("Disabled WASAPI source, reverting to playback mode");
        }
    }
}