using player.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render
{
    class FramebufferRenderTexture
    {
        public int RenderTexture { get { return (int)MainRenderTexture; } }

        uint MainFramebuffer;
        uint MainRenderTexture;
        uint MainDepthRenderBuffer;
        int Width, Height;
        bool resizePending = false;
        FramebufferCreationHooks callbacks;
        FramebufferManager fbManager;
        bool pushedToFbManager;

        public FramebufferRenderTexture(int width, int height, FramebufferCreationHooks hooks = null)
        {
            Width = width;
            Height = height;
            if (hooks == null)
            {
                callbacks = new FramebufferCreationHooks();
            }
            else
            {
                callbacks = hooks;
            }

            fbManager = ServiceManager.GetService<FramebufferManager>();
        }

        public void FinishRendering()
        {
            if (pushedToFbManager)
            {
                fbManager.PopFramebuffer(FramebufferTarget.Framebuffer);
                pushedToFbManager = false;
            }
            GL.PopAttrib();
        }

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            resizePending = true;
        }

        public virtual void Cleanup()
        {
            GL.DeleteTexture(MainRenderTexture);
            GL.DeleteFramebuffer(MainFramebuffer);
            GL.DeleteRenderbuffer(MainDepthRenderBuffer);
            MainRenderTexture = MainFramebuffer = MainDepthRenderBuffer = 0;
        }

        public virtual void BindAndRenderTo(bool pushToFbManager = false)
        {
            if (MainFramebuffer == 0) CreateRenderFramebuffer();
            if (resizePending)
            {
                Cleanup();
                CreateRenderFramebuffer();
                resizePending = false;
            }

            if (pushToFbManager)
            {
                fbManager.PushFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
                pushedToFbManager = true;
            }
            else
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
            }
            GL.PushAttrib(AttribMask.ViewportBit);
            GL.Viewport(0, 0, Width, Height);
        }


        private void CreateRenderFramebuffer()
        {
            GL.GenFramebuffers(1, out MainFramebuffer);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainFramebuffer);
            GL.GenTextures(1, out MainRenderTexture);
            GL.BindTexture(TextureTarget.Texture2D, MainRenderTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            callbacks?.TextureCreationHook?.Invoke();
            GL.GenRenderbuffers(1, out MainDepthRenderBuffer);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, Width, Height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, MainDepthRenderBuffer);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, MainRenderTexture, 0);
            DrawBuffersEnum[] outVal = { DrawBuffersEnum.ColorAttachment0 };
            GL.DrawBuffers(1, outVal);
        }

        public class FramebufferCreationHooks
        {
            public Action TextureCreationHook = null;
        }
    }
}
