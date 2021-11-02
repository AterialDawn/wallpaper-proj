using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using player.Utility;
using Log = player.Core.Logging.Logger;
using System.IO;
using player.Core.Render;
using player.Core.Service;
using player.Shaders;
using OpenTK;

namespace player.Renderers.BarHelpers
{
    class StaticImageBackground : BackgroundBase
    {
        public override bool Animated { get { return false; } }

        private int textureIndex;
        private FramebufferRenderTexture renderTargetHelper;
        private Primitives primitives;

        bool alternativeRenderUsed = false;
        private GaussianBlurShader gaussianBlur;

        static FramebufferRenderTexture.FramebufferCreationHooks framebufferHooks;

        static StaticImageBackground()
        {
            framebufferHooks = new FramebufferRenderTexture.FramebufferCreationHooks { TextureCreationHook = () =>
             {
                 GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
                 GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
             }
            };
        }

        public StaticImageBackground(string sourcePath)
        {
            SourcePath = sourcePath;
            primitives = ServiceManager.GetService<Primitives>();
        }

        public override bool Preload()
        {
            textureIndex = GL.GenTexture();
            try
            {
                using (Bitmap image = new Bitmap(SourcePath))
                {
                    Resolution = new SizeF(image.Width, image.Height);

                    if (Math.Abs((Resolution.Width / Resolution.Height) - (RenderResolution.X / RenderResolution.Y)) > 0.05f)
                    {
                        int targetWidth = (int)(((float)image.Width / (float)image.Height) * (float)RenderResolution.Y);

                        using (Bitmap resized = new Bitmap(targetWidth, (int)RenderResolution.Y))
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            using (ProfilingHelper profiler = new ProfilingHelper("Image Resizing"))
                            {
                                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                                g.DrawImage(image, 0, 0, targetWidth, RenderResolution.Y);
                            }

                            TextureUtils.LoadBitmapIntoTexture(resized, textureIndex);
                            Resolution = new SizeF(targetWidth, RenderResolution.Y);

                            Log.Log($"Re-Rendering, Source image aspect is {Resolution.Width / Resolution.Height}");
                            RenderDifferentSizedImage();
                            alternativeRenderUsed = true;
                        }
                    }
                    else
                    {
                        TextureUtils.LoadBitmapIntoTexture(image, textureIndex);
                    }

                    
                    GL.Finish();
                    return true;
                }
            }
            catch (Exception e)
            {
                ReportPreloadError($"Error loading : {Path.GetFileName(SourcePath)}", e);
            }
            return false;
        }

        public override void Update(double elapsedTime)
        {
            //Nothing to do
        }

        public override void BindTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, alternativeRenderUsed ? renderTargetHelper.RenderTexture : textureIndex );
        }

        public override void Destroy()
        {
            GL.DeleteTexture(textureIndex);
            if (alternativeRenderUsed)
            {
                renderTargetHelper.Cleanup();
            }
        }

        //runs in Preload, in a loadercontext thread
        void RenderDifferentSizedImage()
        {
            GL.PushMatrix();
            GL.LoadIdentity();

            gaussianBlur = new GaussianBlurShader();
            gaussianBlur.SetBlurState(true);
            gaussianBlur.SetResolution(RenderResolution);
            gaussianBlur.SetStrength(3f);

            float scalar = RenderResolution.Y / Resolution.Height;
            float aspect = ((Resolution.Width / Resolution.Height) / (RenderResolution.X / RenderResolution.Y));

            float bgImageScalar = Math.Max(1f, 1f / aspect * 0.75f);

            float horizontalMove = aspect;

            renderTargetHelper = new FramebufferRenderTexture((int)RenderResolution.X, (int)RenderResolution.Y, framebufferHooks);
            var bgTargetHelper = new FramebufferRenderTexture((int)RenderResolution.X, (int)RenderResolution.Y, framebufferHooks);
            bgTargetHelper.BindAndRenderTo();

            GL.BindTexture(TextureTarget.Texture2D, textureIndex);

            GL.Scale(2, 2, 1);

            gaussianBlur.Activate();

            {
                GL.PushMatrix();
                GL.Translate(-horizontalMove, 0, 0);
                GL.Scale(aspect, 1, 1);
                GL.Scale(bgImageScalar, bgImageScalar, 1);
                primitives.CenteredQuad.Draw();
                GL.PopMatrix();
            }
            {
                GL.PushMatrix();
                GL.Translate(horizontalMove, 0, 0);
                GL.Scale(aspect, 1, 1);
                GL.Scale(bgImageScalar, bgImageScalar, 1);
                primitives.CenteredQuad.Draw();
                GL.PopMatrix();
            }

            

            bgTargetHelper.FinishRendering();

            renderTargetHelper.BindAndRenderTo();

            GL.BindTexture(TextureTarget.Texture2D, bgTargetHelper.RenderTexture);

            primitives.CenteredQuad.Draw();

            GL.BindTexture(TextureTarget.Texture2D, textureIndex);

            gaussianBlur.SetBlurState(false);

            {
                GL.PushMatrix();
                GL.Scale(aspect, 1, 1);

                primitives.CenteredQuad.Draw();

                GL.PopMatrix();
            }

            gaussianBlur.SetBlackState(true);
            {
                float targetSizeInPixels = 9f * (RenderResolution.X / 1920f);

                GL.PushMatrix();
                GL.Translate(horizontalMove * 0.5f, -0.5f, 0);
                GL.Scale((1f / RenderResolution.X) * targetSizeInPixels, 1, 1);

                primitives.QuadBuffer.Draw();

                GL.PopMatrix();

                GL.PushMatrix();
                GL.Translate(-horizontalMove * 0.5f, -0.5f, 0);
                GL.Scale((1f / RenderResolution.X) * targetSizeInPixels, 1, 1);

                primitives.QuadBuffer.Draw();

                GL.PopMatrix();
            }

            renderTargetHelper.FinishRendering();

            GL.PopMatrix();

            Resolution = new SizeF(RenderResolution.X, RenderResolution.Y);

            bgTargetHelper.Cleanup();
        }
    }
}
