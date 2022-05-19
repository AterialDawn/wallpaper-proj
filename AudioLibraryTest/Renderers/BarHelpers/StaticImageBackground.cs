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
using player.Core.Settings;
using OpenTK.Graphics;
using System.Drawing.Imaging;

namespace player.Renderers.BarHelpers
{
    class StaticImageBackground : BackgroundBase
    {
        public override bool Animated { get { return false; } }

        private int textureIndex;
        private FramebufferRenderTexture renderTargetHelper;
        private Primitives primitives;
        private VertexFloatBuffer renderBuffer = new VertexFloatBuffer(VertexFormat.XY_UV, bufferHint: BufferUsageHint.StaticDraw);

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
                        var settingsForImage = ServiceManager.GetService<WallpaperImageSettingsService>().GetImageSettingsForPath(SourcePath);
                        int resizedWidth = image.Width, resizedHeight = image.Height;

                        if (settingsForImage != null)
                        {
                            resizedWidth = image.Width - (settingsForImage.TrimPixelsLeft + settingsForImage.TrimPixelsRight);
                            resizedHeight = image.Height - (settingsForImage.TrimPixelsTop + settingsForImage.TrimPixelsBottom);
                        }

                        int targetWidth = (int)Math.Ceiling(((float)resizedWidth / (float)resizedHeight) * (float)RenderResolution.Y);

                        using (Bitmap resized = new Bitmap(targetWidth, (int)RenderResolution.Y))
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

                            using (var wrapMode = new ImageAttributes())
                            {
                                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                                if (settingsForImage != null)
                                {
                                    g.DrawImage(image, new Rectangle(0, 0, targetWidth, (int)RenderResolution.Y), settingsForImage.TrimPixelsLeft, settingsForImage.TrimPixelsTop, resizedWidth, resizedHeight, GraphicsUnit.Pixel, wrapMode);
                                }
                                else
                                {
                                    g.DrawImage(image, new Rectangle(0, 0, targetWidth, (int)RenderResolution.Y), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                                }
                            }

                            TextureUtils.LoadBitmapIntoTexture(resized, textureIndex);
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                            Resolution = new SizeF(targetWidth, RenderResolution.Y);
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
            var settingsForImage = ServiceManager.GetService<WallpaperImageSettingsService>().GetImageSettingsForPath(SourcePath);
            var currentMode = BackgroundMode.BorderedDefault;
            var backgroundColor = System.Numerics.Vector4.One;
            var align = BackgroundAnchorPosition.Center;
            gaussianBlur = new GaussianBlurShader();
            gaussianBlur.SetResolution(RenderResolution);

            if (settingsForImage != null)
            {
                currentMode = settingsForImage.Mode;
                backgroundColor = settingsForImage.BackgroundColor;
                align = settingsForImage.AnchorPosition;
            }

            renderTargetHelper = new FramebufferRenderTexture((int)RenderResolution.X, (int)RenderResolution.Y, framebufferHooks);

            float aspect = ((Resolution.Width / Resolution.Height) / (RenderResolution.X / RenderResolution.Y));

            renderBuffer.Clear();

            GL.PushMatrix();
            GL.LoadIdentity();
            switch (currentMode)
            {
                case BackgroundMode.BorderedDefault:
                    {
                        gaussianBlur.SetBlurState(true);
                        gaussianBlur.SetStrength(3f);

                        float scalar = RenderResolution.Y / Resolution.Height;

                        float bgImageScalar = Math.Max(1f, 1f / aspect * 0.75f);

                        float horizontalMove = aspect;
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

                        gaussianBlur.SetColorOverride(true, new Vector4(0,0,0,1));
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

                        bgTargetHelper.Cleanup();
                        break;
                    }
                case BackgroundMode.SolidBackground:
                    {
                        renderBuffer.AddVertex(0f, 0f, 0f, 0f);
                        renderBuffer.AddVertex(0f, Resolution.Height, 0f, 1f);
                        renderBuffer.AddVertex(Resolution.Width, Resolution.Height, 1f, 1f);
                        renderBuffer.AddVertex(0f, 0f, 0f, 0f);
                        renderBuffer.AddVertex(Resolution.Width, Resolution.Height, 1f, 1f);
                        renderBuffer.AddVertex(Resolution.Width, 0f, 1f, 0f);
                        renderBuffer.Load();

                        renderTargetHelper.BindAndRenderTo();

                        gaussianBlur.SetColorOverride(true, new Vector4(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, backgroundColor.W));
                        GL.PushMatrix();
                        GL.Scale(2, 2, 1);
                        primitives.CenteredQuad.Draw();
                        GL.PopMatrix();

                        gaussianBlur.SetColorOverride(false, Vector4.Zero);

                        GL.Ortho(0, RenderResolution.X, 0, RenderResolution.Y, -1, 1);

                        GL.BindTexture(TextureTarget.Texture2D, textureIndex);

                        {

                            switch (align)
                            {
                                case BackgroundAnchorPosition.Center:
                                    GL.Translate((int)((RenderResolution.X * 0.5f) - (Resolution.Width * 0.5f)), 0, 0);
                                    break;

                                case BackgroundAnchorPosition.Right:
                                    GL.Translate((int)(RenderResolution.X - Resolution.Width), 0, 0);
                                    break;
                            }

                            renderBuffer.Draw();
                        }

                        renderTargetHelper.FinishRendering();
                        break;
                    }
            }

            GL.PopMatrix();

            Resolution = new SizeF(RenderResolution.X, RenderResolution.Y);
        }
    }
}
