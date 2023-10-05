using OpenTK;
using player.Core.Render;
using player.Core.Service;
using System;
using System.Drawing;
using System.IO;
using Log = player.Core.Logging.Logger;

namespace player.Renderers.BarHelpers
{
    abstract class BackgroundBase : IBackground
    {
        public abstract bool Animated { get; }

        public SizeF Resolution { get; protected set; }

        public Vector2 RenderResolution { get; set; }

        public string SourcePath { get; protected set; }
        public TimeSpan? OverrideBackgroundDuration { get; protected set; } = null;

        public double? OverrideFps { get; protected set; } = null;

        public abstract void BindTexture();

        public abstract void Destroy();

        public abstract bool Preload();

        public abstract void Update(double elapsedTime);

        protected void ReportPreloadError(string message, Exception e = null)
        {
            string exceptionText = (e != null ? e.ToString() : "No exception!");
            Log.Log("Exception attempting to load image {0}! E : {1}", SourcePath, exceptionText);
            ServiceManager.GetService<MessageCenterService>().ShowMessage($"Error loading : {Path.GetFileName(SourcePath)}");
        }

        protected void ShowMessageCenterMessage(string message)
        {
            ServiceManager.GetService<MessageCenterService>().ShowMessage(message);
        }
    }
}
