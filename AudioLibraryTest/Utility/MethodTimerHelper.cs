using player.Core.Render;
using player.Core.Render.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Utility
{
    class MethodTimerHelper
    {
        private Action method;
        private string displayName;
        private OLabel displayMessage;
        double averageMs = 0;
        double peak = 0;
        int timesWithoutPeak = 0;

        public MethodTimerHelper(string displayName, Action method)
        {
            this.method = method;
            this.displayName = displayName;
            displayMessage = new OLabel($"MTH: {displayName}", "", QuickFont.QFontAlignment.Left, true);
        }

        public void UpdateMethod(Action method)
        {
            this.method = method;
        }

        public void ExecuteAndTime()
        {
            double currentTimeMs = UtilityMethods.TimeMethod(method);

            if (averageMs == 0) averageMs = currentTimeMs;
            if (currentTimeMs > peak)
            {
                peak = currentTimeMs;
                timesWithoutPeak = 0;
            }
            timesWithoutPeak++;
            if (timesWithoutPeak > 400)
            {
                peak = currentTimeMs;
                timesWithoutPeak = 0;
            }
            displayMessage.Text = $"{displayName} : {averageMs.ToString("0000.00")} / {currentTimeMs.ToString("0000")} / {peak.ToString("0000")}";

            if (Math.Abs(averageMs - currentTimeMs) > averageMs * 0.8)
            {
                //displayMessage.Color = new OpenTK.Graphics.Color4(0, 0, 255, 255);
            }
            else
            {
                //displayMessage.Color = new OpenTK.Graphics.Color4(255, 255, 255, 255);
            }

            averageMs = UtilityMethods.LinearInterpolate(averageMs, currentTimeMs, 0.05);
        }
    }
}
