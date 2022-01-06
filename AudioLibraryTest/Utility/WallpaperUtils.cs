using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using player.Core;
using Log = player.Core.Logging.Logger;

namespace player.Utility
{
    static class WallpaperUtils
    {
        public static bool IsWallpaperEnabled { get; private set; }
        public static bool IsWin8Plus { get; private set; }

        /// <summary>
        /// Wallpaper bounds. Not corrected. 0,0,0,0 is far left of all monitors
        /// </summary>
        public static Rectangle WallpaperBounds { get; private set; } = Rectangle.Empty;
        /// <summary>
        /// Wallpaper bounds. Corrected. 0,0,0,0 is far left of PRIMARY monitor ONLY. Can be negative.
        /// </summary>
        public static Rectangle WallpaperBoundsCorrected { get; private set; } = Rectangle.Empty;

        public static void EnableWallpaperMode()
        {
            if (IsWallpaperEnabled) return;
            GetDesktopBounds();
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
            {
                IsWin8Plus = true;
                Log.Log("OS >= Win8 Detected, Enabling Win8WallpaperMode");
                EnableWallpaperModeWin8();
            }
            else
            {
                Log.Log("OS < Win8 detected, unable to render below icons");
                EnableWallpaperModeLT8();
            }
            IsWallpaperEnabled = true;
        }

        public static void DisableWallpaperMode()
        {
            if (IsWallpaperEnabled)
            {
                IsWallpaperEnabled = false;
                IntPtr selfHandle = VisGameWindow.ThisForm.GetHandleOfGameWindow(true);
                W32.SetParent(selfHandle, IntPtr.Zero);
            }
        }

        public static void SendToBottom()
        {
            VisGameWindow thisForm = VisGameWindow.ThisForm;
            Win32.SetWindowPos(thisForm.GetHandleOfGameWindow(true), Win32.SetWindowPosLocationFlags.HWND_BOTTOM, 0, 0, 0, 0, Win32.SetWindowPosFlags.IgnoreResize | Win32.SetWindowPosFlags.IgnoreMove | Win32.SetWindowPosFlags.DoNotActivate);
        }

        static void EnableWallpaperModeLT8()
        {
            VisGameWindow thisForm = VisGameWindow.ThisForm;
            IntPtr selfHandle = thisForm.GetHandleOfGameWindow(true);

            thisForm.WindowBorder = OpenTK.WindowBorder.Hidden;

            Rectangle bounds = WallpaperBounds;
            thisForm.X += bounds.X;
            thisForm.Y += bounds.Y;
            thisForm.Width = bounds.Width;
            thisForm.Height = bounds.Height;

            int exStyle = (int)Win32.GetWindowLong(selfHandle, (int)Win32.GetWindowLongFields.GWL_EXSTYLE);

            Win32.ShowWindow(selfHandle, Win32.ShowWindowCommands.Hide);

            exStyle |= (int)Win32.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            exStyle &= ~(int)Win32.ExtendedWindowStyles.WS_EX_APPWINDOW;
            Win32.SetWindowLong(selfHandle, (int)Win32.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            //Win32.SetWindowPos(thisForm.GetHandleOfGameWindow(false), Win32.SetWindowPosLocationFlags.HWND_BOTTOM, 0, 0, 0, 0, Win32.SetWindowPosFlags.IgnoreResize | Win32.SetWindowPosFlags.IgnoreMove | Win32.SetWindowPosFlags.DoNotActivate);
            IntPtr hprog = W32.FindWindowEx(W32.FindWindowEx(W32.FindWindow("Progman", "Program Manager"),IntPtr.Zero, "SHELLDLL_DefView", ""), IntPtr.Zero, "SysListView32", "FolderView");
            if (hprog == null) throw new InvalidOperationException("Unable to find the desktop!");
            W32.SetParent(selfHandle, hprog);

            //W32.SetWindowLong(selfHandle, W32.WindowLongFlags.GWLP_HWNDPARENT, hprog);
        }

        internal static void GetDesktopBounds()
        {
            int xOff = 0, yOff = 0;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.X < xOff) xOff = screen.Bounds.X;
                if (screen.Bounds.Y < yOff) yOff = screen.Bounds.Y;
            }

            Point wallpaperModePosition = Screen.PrimaryScreen.Bounds.Location;
            Size wallpaperModeSize = Screen.PrimaryScreen.Bounds.Size;
            var wallpaperPosOption = Program.CLIParser.ActiveOptions.Where(o => o.Item1.Equals("WallpaperPos")).FirstOrDefault();
            var wallpaperSizeOption = Program.CLIParser.ActiveOptions.Where(o => o.Item1.Equals("WallpaperSize")).FirstOrDefault();
            if (wallpaperPosOption != null)
            {
                string[] split = wallpaperPosOption.Item2.Split('x');
                if (split.Length == 2)
                {
                    wallpaperModePosition = new Point(int.Parse(split[0]), int.Parse(split[1]));
                }
            }
            if (wallpaperSizeOption != null)
            {
                string[] split = wallpaperSizeOption.Item2.Split('x');
                if (split.Length == 2)
                {
                    wallpaperModeSize = new Size(int.Parse(split[0]), int.Parse(split[1]));
                }
            }
            WallpaperBounds = new Rectangle(wallpaperModePosition, wallpaperModeSize);
            WallpaperBoundsCorrected = new Rectangle(xOff + WallpaperBounds.X, yOff + WallpaperBounds.Y, WallpaperBounds.Width, WallpaperBounds.Height);
            Log.Log($"Wallpaper bounds : {WallpaperBounds}. Corrected : {WallpaperBoundsCorrected}");
        }

        public static void BeforeClose()
        {
            if (IsWin8Plus)
            {
                SetDesktopWallpaper(GetDesktopWallpaper()); //When player is closed, the last frame will remain, resetting the wallpaper clears it
            }
        }

        static string GetDesktopWallpaper()
        {
            string wallpaper = new string('\0', Win32.MAX_PATH);
            W32.SystemParametersInfo(Win32.SPI_GETDESKWALLPAPER, (uint)wallpaper.Length, wallpaper, 0);
            return wallpaper.Substring(0, wallpaper.IndexOf('\0'));
        }

        static void SetDesktopWallpaper(string filename)
        {
            W32.SystemParametersInfo(Win32.SPI_SETDESKWALLPAPER, 0, filename,
                Win32.SPIF_UPDATEINIFILE | Win32.SPIF_SENDWININICHANGE);
        }

        static void EnableWallpaperModeWin8()
        {
            VisGameWindow thisForm = VisGameWindow.ThisForm;

            IntPtr progman = W32.FindWindow("Progman", null);

            IntPtr result = IntPtr.Zero;

            // Send 0x052C to Progman. This message directs Progman to spawn a 
            // WorkerW behind the desktop icons. If it is already there, nothing 
            // happens.
            W32.SendMessageTimeout(progman,
                                   0x052C,
                                   new IntPtr(0),
                                   IntPtr.Zero,
                                   W32.SendMessageTimeoutFlags.SMTO_NORMAL,
                                   1000,
                                   out result);

            IntPtr workerw = IntPtr.Zero;

            // We enumerate all Windows, until we find one, that has the SHELLDLL_DefView 
            // as a child. 
            // If we found that window, we take its next sibling and assign it to workerw.
            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            IntPtr.Zero);
                //Log.Log("P = {0:X8}", p);

                if (p != IntPtr.Zero)
                {
                    // Gets the WorkerW Window after the current one.
                    workerw = W32.FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               IntPtr.Zero);

                }

                return true;
            }), IntPtr.Zero);
            thisForm.WindowBorder = OpenTK.WindowBorder.Hidden;
            Rectangle bounds = WallpaperBounds;
            thisForm.X = bounds.X;
            thisForm.Y = bounds.Y;
            thisForm.Width = bounds.Width;
            thisForm.Height = bounds.Height;

            IntPtr selfHandle = thisForm.GetHandleOfGameWindow(true);
            var parentPtr = W32.SetParent(selfHandle, workerw);
        }
    }
}
