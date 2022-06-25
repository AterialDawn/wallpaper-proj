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
using player.Core;

namespace player.Renderers.BarHelpers
{
    class StaticImageBackground : BackgroundBase
    {
        public override bool Animated { get { return false; } }
        public Size SourceImageSize { get; private set; } = Size.Empty;

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
                    SourceImageSize = image.Size;
                    var settingsForImage = ServiceManager.GetService<WallpaperImageSettingsService>().GetImageSettingsForPath(SourcePath);
                    if (settingsForImage != null)
                    {
                        Resolution = new SizeF(
                            image.Width - (settingsForImage.TrimPixelsLeft + settingsForImage.TrimPixelsRight),
                            image.Height - (settingsForImage.TrimPixelsTop + settingsForImage.TrimPixelsBottom));
                    }
                    else
                    {
                        Resolution = new SizeF(image.Width, image.Height);
                    }

                    if (Math.Abs((Resolution.Width / Resolution.Height) - (RenderResolution.X / RenderResolution.Y)) > 0.05f)
                    {
                        int targetWidth = (int)Math.Ceiling((Resolution.Width / Resolution.Height) * (float)RenderResolution.Y);

                        using (Bitmap resized = new Bitmap(targetWidth, (int)RenderResolution.Y))
                        using (Graphics g = Graphics.FromImage(resized))
                        {
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

                            if (settingsForImage != null)
                            {
                                using (var brush = new SolidBrush(Color.FromArgb(
                                    (int)(settingsForImage.BackgroundColor.W * 255),
                                    (int)(settingsForImage.BackgroundColor.X * 255),
                                    (int)(settingsForImage.BackgroundColor.Y * 255),
                                    (int)(settingsForImage.BackgroundColor.Z * 255))))
                                {
                                    g.FillRectangle(brush, 0, 0, targetWidth, RenderResolution.Y);
                                }
                            }

                            using (var wrapMode = new ImageAttributes())
                            {
                                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                                if (settingsForImage != null)
                                {
                                    g.DrawImage(image, new Rectangle(0, 0, targetWidth, (int)RenderResolution.Y), settingsForImage.TrimPixelsLeft, settingsForImage.TrimPixelsTop, Resolution.Width, Resolution.Height, GraphicsUnit.Pixel, wrapMode);
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

            VertexFloatBuffer renderBuffer = new VertexFloatBuffer(VertexFormat.XY_UV, bufferHint: BufferUsageHint.StreamDraw);

            GL.PushMatrix();
            GL.LoadIdentity();
            switch (currentMode)
            {
                case BackgroundMode.BorderedDefault:
                    {
                        gaussianBlur.SetBlurState(true);

                        float bgImageScalar = Math.Max(1f, 1f / aspect * 0.75f);

                        float horizontalMove = aspect;
                        var bgTargetHelper = new FramebufferRenderTexture((int)RenderResolution.X, (int)RenderResolution.Y, framebufferHooks);
                        bgTargetHelper.BindAndRenderTo();

                        GL.BindTexture(TextureTarget.Texture2D, textureIndex);

                        GL.Scale(2, 2, 1);

                        gaussianBlur.Activate();

                        {
                            GL.Enable(EnableCap.ScissorTest);
                            GL.Scissor(0, 0, (int)(RenderResolution.X * 0.5f), (int)RenderResolution.Y);
                            GL.PushMatrix();
                            GL.Translate(-horizontalMove, 0, 0);
                            GL.Scale(aspect, 1, 1);
                            GL.Scale(bgImageScalar, bgImageScalar, 1);
                            primitives.CenteredQuad.Draw();
                            GL.PopMatrix();
                        }
                        {
                            GL.Scissor((int)(RenderResolution.X * 0.5f), 0, (int)(RenderResolution.X * 0.5f), (int)RenderResolution.Y);
                            GL.PushMatrix();
                            GL.Translate(horizontalMove, 0, 0);
                            GL.Scale(aspect, 1, 1);
                            GL.Scale(bgImageScalar, bgImageScalar, 1);
                            primitives.CenteredQuad.Draw();
                            GL.PopMatrix();
                            GL.Disable(EnableCap.ScissorTest);
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
                            float barWidth = (1f / RenderResolution.X) * targetSizeInPixels;

                            GL.PushMatrix();
                            GL.Translate(horizontalMove * 0.5f, -0.5f, 0);
                            GL.Scale(barWidth, 1, 1);

                            primitives.QuadBuffer.Draw();

                            GL.PopMatrix();

                            GL.PushMatrix();
                            GL.Translate(-horizontalMove * 0.5f - barWidth, -0.5f, 0);
                            GL.Scale(barWidth, 1, 1);

                            primitives.QuadBuffer.Draw();

                            GL.PopMatrix();
                        }

                        renderTargetHelper.FinishRendering();

                        bgTargetHelper.Cleanup();
                        break;
                    }
                case BackgroundMode.SolidBackground:
                    {
                        float normPixelX = 1f / Resolution.Width;
                        float normPixelY = 1f / Resolution.Height;
                        float imageWidth = Resolution.Width;
                        float imageHeight = Resolution.Height;
                        if (settingsForImage != null &&
                            (
                            settingsForImage.RenderTrimBot != 0 ||
                            settingsForImage.RenderTrimTop != 0 ||
                            settingsForImage.RenderTrimLeft != 0 ||
                            settingsForImage.RenderTrimRight != 0)
                            )
                        {
                            float normBot = settingsForImage.RenderTrimBot * normPixelY;

                            float top = imageHeight;

                            float normTop = 1f - (settingsForImage.RenderTrimTop * normPixelY);
                            float normLeft = settingsForImage.RenderTrimLeft * normPixelX;

                            float right = imageWidth - (settingsForImage.RenderTrimLeft + settingsForImage.RenderTrimRight);
                            float normRight = 1f - (settingsForImage.RenderTrimRight * normPixelX);

                            renderBuffer.AddVertex(0, 0, normLeft, normBot);
                            renderBuffer.AddVertex(0, imageHeight, normLeft, normTop);
                            renderBuffer.AddVertex(right, top, normRight, normTop);
                            renderBuffer.AddVertex(0, 0, normLeft, normBot);
                            renderBuffer.AddVertex(right, top, normRight, normTop);
                            renderBuffer.AddVertex(right, 0, normRight, normBot);
                            renderBuffer.Load();

                            imageWidth = imageWidth - (settingsForImage.RenderTrimLeft + settingsForImage.RenderTrimRight);
                        }
                        else
                        {
                            renderBuffer.AddVertex(0f, 0f, 0f, 0f);
                            renderBuffer.AddVertex(0f, Resolution.Height, 0f, 1f);
                            renderBuffer.AddVertex(Resolution.Width, Resolution.Height, 1f, 1f);
                            renderBuffer.AddVertex(0f, 0f, 0f, 0f);
                            renderBuffer.AddVertex(Resolution.Width, Resolution.Height, 1f, 1f);
                            renderBuffer.AddVertex(Resolution.Width, 0f, 1f, 0f);
                            renderBuffer.Load();
                        }


                        renderTargetHelper.BindAndRenderTo();

                        GL.Ortho(0, RenderResolution.X, 0, RenderResolution.Y, -1, 1);

                        if (settingsForImage.BackgroundStyle == SolidBackgroundStyle.SolidColor)
                        {
                            gaussianBlur.SetColorOverride(true, new Vector4(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, backgroundColor.W));
                            GL.PushMatrix();
                            GL.Scale(RenderResolution.X, RenderResolution.Y, 1);
                            primitives.QuadBuffer.Draw();
                            GL.PopMatrix();
                        }
                        else if (settingsForImage.BackgroundStyle == SolidBackgroundStyle.StretchEdge)
                        {
                            GL.BindTexture(TextureTarget.Texture2D, textureIndex);
                            gaussianBlur.SetColorOverride(false, Vector4.One);
                            //flip uv coordinates of left rect
                            float uvSampleWidth = settingsForImage.StretchWidth / RenderResolution.X;
                            float uvLeftSamplePos = (settingsForImage.StretchXPos / RenderResolution.X) + uvSampleWidth;

                            if (settingsForImage.AnchorPosition == BackgroundAnchorPosition.Center)
                            {
                                VertexFloatBuffer leftRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF(0, 0, (int)((RenderResolution.X * 0.5f) - (imageWidth * 0.5f)), RenderResolution.Y),
                                    new RectangleF(uvLeftSamplePos, 0, -uvSampleWidth, 1));

                                leftRect.Draw();

                                VertexFloatBuffer rightRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF((int)((RenderResolution.X * 0.5f) + (imageWidth * 0.5f)), 0, (int)((RenderResolution.X * 0.5f) - (imageWidth * 0.5f)), RenderResolution.Y),
                                    new RectangleF(1f - ((settingsForImage.StretchXPos + settingsForImage.StretchWidth) / RenderResolution.X), 0, uvSampleWidth, 1));

                                rightRect.Draw();

                                rightRect.Unload();
                                leftRect.Unload();
                            }
                            else if (settingsForImage.AnchorPosition == BackgroundAnchorPosition.Left)
                            {
                                var rightRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF(imageWidth, 0, RenderResolution.X - imageWidth, RenderResolution.Y),
                                    new RectangleF(1f - ((settingsForImage.StretchXPos + settingsForImage.StretchWidth) / RenderResolution.X), 0, uvSampleWidth, 1));

                                rightRect.Draw();
                                rightRect.Unload();
                            }
                            else if(settingsForImage.AnchorPosition == BackgroundAnchorPosition.Right)
                            {
                                var leftRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF(0, 0, RenderResolution.X - imageWidth, RenderResolution.Y),
                                    new RectangleF(uvLeftSamplePos, 0, -uvSampleWidth, 1));

                                leftRect.Draw();
                                leftRect.Unload();
                            }
                        }

                        gaussianBlur.SetColorOverride(false, Vector4.Zero);

                        GL.BindTexture(TextureTarget.Texture2D, textureIndex);

                        {

                            switch (align)
                            {
                                case BackgroundAnchorPosition.Center:
                                    GL.Translate((int)((RenderResolution.X * 0.5f) - (imageWidth * 0.5f)), 0, 0);
                                    break;

                                case BackgroundAnchorPosition.Right:
                                    GL.Translate((int)(RenderResolution.X - imageWidth), 0, 0);
                                    break;
                            }

                            renderBuffer.Draw();
                        }

                        renderTargetHelper.FinishRendering();
                        break;
                    }
            }
            renderBuffer.Unload();

            GL.PopMatrix();

            Resolution = new SizeF(RenderResolution.X, RenderResolution.Y);
        }
    }
}
