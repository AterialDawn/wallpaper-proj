using System.Collections.Generic;
using System.Diagnostics;
using player.Utility;
using player.Core.Service;
using System;
using player.Core.Render.UI.Controls;
using player.Core.Audio;

namespace player.Core.Render
{
    public class FpsTracker : IService
    {
        public float Fps { get { return fps; } }

        int frameCount = 0;
        double currentTime;
        double previousTime;
        float fps;
        float lastFps;
        Stopwatch _stopwatch = new Stopwatch();

        public string ServiceName { get { return "FpsTracker"; } }

        private List<string> ExtraMessageList = new List<string>();
        double fpsTimeStart;
        const int NewFpsCalcMs = 500;
        private OLabel fpsLabel;
        private OLabel cpuUsageLabel;
        private SoundDataProcessor sdp;

        internal FpsTracker()
        {
            
        }

        public void Initialize()
        {
            _stopwatch.Start();
            fpsLabel = new OLabel("FPS Label", "0.00", QuickFont.QFontAlignment.Left, true);
            cpuUsageLabel = new OLabel("CPU Usage", "0.00% | 0.00%", QuickFont.QFontAlignment.Left, true);
            cpuUsageLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            cpuUsageLabel.AutoSize = true;
            fpsLabel.AutoSize = true;
            sdp = ServiceManager.GetService<SoundDataProcessor>();
        }

        public void Cleanup()
        {

        }

        public void Update()
        {
            frameCount++;

            currentTime = _stopwatch.Elapsed.TotalMilliseconds;

            int timeInterval = (int)(currentTime - previousTime);

            if (timeInterval > NewFpsCalcMs)
            {
                UpdateFps(timeInterval);
            }
            UpdateSmoothedFps();
        }

        private void UpdateFps(int timeInterval)
        {
            lastFps = fps;
            //  calculate the number of frames per second
            fps = frameCount / ((float)timeInterval / 1000f);
            //  Set time
            previousTime = currentTime;

            //  Reset frame count
            frameCount = 0;

            fpsTimeStart = _stopwatch.Elapsed.TotalMilliseconds;
        }

        private void UpdateSmoothedFps()
        {
            double currentElapsedMs = _stopwatch.Elapsed.TotalMilliseconds - fpsTimeStart;

            float FpsSmoothingMult = (float)UtilityMethods.Clamp((currentElapsedMs / (double)NewFpsCalcMs), 0f, 1f);
            FpsSmoothingMult = EasingMethods.QuadOut(FpsSmoothingMult);

            float SmoothedFps = UtilityMethods.LinearInterpolate(lastFps, fps, FpsSmoothingMult);

            fpsLabel.Text = $"{SmoothedFps:0.00}";
            if (Program.DisableAudioProcessing)
            {
                cpuUsageLabel.Text = $"{VisGameWindow.ThisForm.FpsLimiter.EstimatedCPUUsage:P2}";
            }
            else
            {
                cpuUsageLabel.Text = $"{VisGameWindow.ThisForm.FpsLimiter.EstimatedCPUUsage:P2} | {sdp.FpsLimiter.EstimatedCPUUsage:P2}";
            }
        }
    }
}
