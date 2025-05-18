using OpenTK.Graphics.OpenGL;
using player.Core.FFmpeg;
using player.Core.Render;
using player.Core.Service;
using System;
using Log = player.Core.Logging.Logger;

namespace player.Renderers.BarHelpers.VideoRenderers
{
    abstract class BaseVideoRenderer
    {
        protected const int FRAME_BUFFER_SIZE = 5;
        public int RenderedVideoTexture { get { return MainRenderTexture; } }

        protected FFMpegDecoder Decoder;

        protected uint MainFramebuffer;
        protected int MainRenderTexture;
        protected uint MainDepthRenderBuffer;
        protected FramebufferManager fbManager;

        protected int Width, Height;

        protected Primitives Primitives;

        protected int currentTextureIndex = 0;
        protected int currentWriteIndex = 0;

        protected int skippedFrames = 0;

        public BaseVideoRenderer(FFMpegDecoder decoder, int width, int height)
        {
            Decoder = decoder;
            Width = width;
            Height = height;
            fbManager = ServiceManager.GetService<FramebufferManager>();
            Primitives = ServiceManager.GetService<Primitives>();
        }

        /// <summary>
        /// Run from ThreadedLoaderContext WHILE STILL IN PRELOAD, SUBCLASSES SHOULD PRELOAD 3? FRAMES FROM DECODER INTO TEXTURES
        /// </summary>
        public abstract void PreloadFrames();

        /// <summary>
        /// Called from main thread, right before RenderNextFrame if some frames need to be skipped
        /// </summary>
        /// <param name="framesToSkip"></param>
        public void SkipFrames(int framesToSkip)
        {
            skippedFrames = framesToSkip;
        }

        public virtual void Cleanup()
        {
            GL.DeleteTexture(MainRenderTexture);
            GL.DeleteFramebuffer(MainFramebuffer);
            GL.DeleteRenderbuffer(MainDepthRenderBuffer);
        }

        public virtual void RenderNextFrame()
        {
            if (MainFramebuffer == 0) CreateRenderFramebuffer();

            fbManager.PushFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
            GL.PushAttrib(AttribMask.ViewportBit);
            GL.Viewport(0, 0, Width, Height);
        }


        private void CreateRenderFramebuffer()
        {
            GL.GenFramebuffers(1, out MainFramebuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
            GL.GenTextures(1, out MainRenderTexture);
            GL.BindTexture(TextureTarget.Texture2D, MainRenderTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            GL.GenRenderbuffers(1, out MainDepthRenderBuffer);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, MainRenderTexture, 0);
            DrawBuffersEnum[] outVal = { DrawBuffersEnum.ColorAttachment0 };
            GL.DrawBuffers(1, outVal);
            Log.Log("FrameBuffer status = {0}", GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ToString());
        }
    }
}
