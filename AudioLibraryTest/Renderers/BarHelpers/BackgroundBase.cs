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
        /// <summary>
        /// If animated, indicates this texture was just updated/changed
        /// </summary>
        public bool Dirty { get; protected set; } = false;

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
            Log.Log($"Exception attempting to load image {SourcePath}! {message} | E : {exceptionText}");
            ServiceManager.GetService<MessageCenterService>().ShowMessage(message);
        }

        protected void ShowMessageCenterMessage(string message)
        {
            ServiceManager.GetService<MessageCenterService>().ShowMessage(message);
        }
    }
}
