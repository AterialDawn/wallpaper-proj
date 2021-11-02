using player.Core;
using player.Core.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Utility
{
    /// <summary>
    /// Basically just a handy wrapper around calling sc.exe and killing shadowplay manually so it doesn't hook into this app
    /// </summary>
    static class ShadowplayUtil
    {
        public static bool WasShadowplayKilled { get; private set; }
        public static bool WasShadowplayRestarted { get; private set; }
        public static bool AccessDenied { get; private set; }
        public static void KillIfNeeded()
        {
            if (VisGameWindow.FormWallpaperMode == WallpaperMode.None) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("sc.exe", "query NvContainerLocalSystem");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                Process sc = Process.Start(psi);

                sc.WaitForExit();

                string output = sc.StandardOutput.ReadToEnd();

                if (output.Contains("4  RUNNING"))
                {
                    Logger.Log("Shadowplay service detected, killing shadowplay");
                    KillShadowplay();
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Shadowplay KillIfNeeded exc! {e}");
            }
        }

        static void KillShadowplay()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("sc.exe", "stop NvContainerLocalSystem");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                Process sc = Process.Start(psi);

                sc.WaitForExit();

                string output = sc.StandardOutput.ReadToEnd();

                if (output.Contains("3  STOP_PENDING"))
                {
                    Logger.Log("Shadowplay was successfully killed");
                    WasShadowplayKilled = true;
                }
                else if (output.Contains("Access is denied"))
                {
                    Logger.Log("Access denied for sc.exe stop :(");
                    AccessDenied = true;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Shadowplay KillShadowplay exc! {e}");
            }
        }

        public static void StartIfNeeded()
        {
            if (!WasShadowplayKilled) return;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("sc.exe", "start NvContainerLocalSystem");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                Process sc = Process.Start(psi);

                sc.WaitForExit();

                string output = sc.StandardOutput.ReadToEnd();

                if (output.Contains("2  START_PENDING"))
                {
                    Logger.Log("Shadowplay was successfully restarted!");
                    WasShadowplayRestarted = true;
                }
                else
                {
                    string stateLine = output.Split('\n').Where(l => { l.Trim('\r'); if (l.Contains("STATE")) return true; return false; }).FirstOrDefault(); //gnarly :)
                    Logger.Log($"Shadowplay may not have restarted successfully? State = {stateLine ?? "No state found?"}");
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Shadowplay KillShadowplay exc! {e}");
            }
        }
    }
}
