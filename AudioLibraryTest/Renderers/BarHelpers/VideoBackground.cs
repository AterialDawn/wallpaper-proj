using OpenTK.Graphics.OpenGL;
using player.Core.FFmpeg;
using player.Core.Render;
using player.Core.Service;
using player.Renderers.BarHelpers.VideoRenderers;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Log = player.Core.Logging.Logger;

namespace player.Renderers.BarHelpers
{
    class VideoBackground : BackgroundBase
    {
        public override bool Animated { get { return true; } }

        private double frameTime = 0;
        private FFMpegDecoder decoder;
        private BaseVideoRenderer videoRenderer;
        private FramebufferManager fbManager;

        public VideoBackground(string sourcePath)
        {
            SourcePath = sourcePath;

            decoder = FFMpegManager.Instance.GetDecoder(SourcePath);

            fbManager = ServiceManager.GetService<FramebufferManager>();
            //decoder.DecodingThreadException += decoder_DecodingThreadException;
            //decoder.TimesToLoop = int.MaxValue;
        }

        public override bool Preload()
        {
            decoder.StartDecoding();

            if (!decoder.WaitUntilFramesDecoded(-1))
            {
                ReportPreloadError($"Error loading : {Path.GetFileName(SourcePath)}");
                return false;
            }
            else
            {
                OverrideBackgroundDuration = decoder.VideoLength;
                OverrideFps = 1d / decoder.FrameDelay;
                LoadInitialFrames();
                Dirty = true;
            }
            return true;
        }

        public override void Update(double elapsedTime)
        {
            Dirty = false;
            frameTime -= elapsedTime;

            if (frameTime <= 0)
            {
                frameTime += decoder.FrameDelay;
                int framesToSkip = 0;
                while (frameTime <= 0)
                {
                    frameTime += decoder.FrameDelay;
                    framesToSkip++;
                }

                videoRenderer.SkipFrames(framesToSkip);
                videoRenderer.RenderNextFrame();
                Dirty = true;
                fbManager.PopFramebuffer(FramebufferTarget.Framebuffer);
                GL.PopAttrib();
            }
        }

        public override int GetTextureIndex() => videoRenderer.RenderedVideoTexture;

        public override void Destroy()
        {
            videoRenderer.Cleanup();

            //The decoder is thread-safe.
            Task.Run(() =>
            {
                decoder.StopDecoding();
                decoder.Dispose();
            });
        }

        void LoadInitialFrames()
        {
            BaseFFMpegFrameContainer container = decoder.GetFrame<BaseFFMpegFrameContainer>();
            Resolution = new SizeF(container.Width, container.Height);
            if (container is FFMpegGBRPFrameContainer gbrFrame)
            {
                videoRenderer = new GBRPlanarVideoRenderer(decoder, gbrFrame);
            }
            else if (container is FFMpegYUV444FrameContainer yuv444pFrame) //444 inherits from 420, move it up higher
            {
                videoRenderer = new YUV444PlanarVideoRenderer(decoder, yuv444pFrame);
            }
            else if (container is FFMpegYUV420FrameContainer yuv420pFrame)
            {
                videoRenderer = new YUV420PlanarVideoRenderer(decoder, yuv420pFrame);
            }


            videoRenderer.PreloadFrames();
        }

        void decoder_DecodingThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.Log("FFMpeg decoding exception! {0}", e.Exception.ToString());
        }
    }
}
