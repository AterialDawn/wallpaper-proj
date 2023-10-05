using OpenTK;
using System;
using System.Drawing;

namespace player.Renderers.BarHelpers
{
    delegate void PreloadCompleteCallback(IBackground sender, bool success);

    interface IBackground
    {
        /// <summary>
        /// Resolution of the background. Will not be used until preload's callback is called with a successful value
        /// </summary>
        SizeF Resolution { get; }

        /// <summary>
        /// The Source Path used to create this IBackground instance
        /// </summary>
        string SourcePath { get; }

        /// <summary>
        /// Resolution of the engine (window res)
        /// </summary>
        Vector2 RenderResolution { get; set; }

        /// <summary>
        /// If not null, will suggest to the controller system the duration this BG should display for
        /// </summary>
        TimeSpan? OverrideBackgroundDuration { get; }

        /// <summary>
        /// If not null, will suggest to controller the FPS the player should run at
        /// </summary>
        double? OverrideFps { get; }

        /// <summary>
        /// Is the background animated?
        /// </summary>
        bool Animated { get; }

        /// <summary>
        /// Begin preloading the background on the ThreadedLoaderContext. This runs asynchrously from the main rendering thread but is allowed to use OpenGL methods
        /// </summary>
        /// <param name="callback"></param>
        /// <returns>True if background is loaded, false otherwise</returns>
        bool Preload();

        /// <summary>
        /// Called when a background should update itself
        /// </summary>
        /// <param name="elapsedTime"></param>
        void Update(double elapsedTime);

        /// <summary>
        /// This should only consist of calling GL.BindTexture with the current texture to use
        /// </summary>
        void BindTexture();

        /// <summary>
        /// Destroy all allocated resources, as the background is no longer used
        /// </summary>
        void Destroy();
    }
}
