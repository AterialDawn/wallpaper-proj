using OpenTK;
using OpenTK.Graphics.OpenGL;
using player.Core;
using player.Core.Render;
using player.Core.Service;
using player.Core.Settings;
using player.Shaders;
using player.Utility;
using System;
using System.Drawing;

namespace player.Renderers.BarHelpers
{
    class BackgroundRenderer
    {
        static FramebufferRenderTexture.FramebufferCreationHooks framebufferHooks;

        static BackgroundRenderer()
        {
            framebufferHooks = new FramebufferRenderTexture.FramebufferCreationHooks
            {
                TextureCreationHook = () =>
                {
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.MirroredRepeat);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.MirroredRepeat);
                }
            };
        }

        public IBackground Background { get { return _background; } set { _background = value; dirty = true; } }
        private IBackground _background;
        public BackgroundController Controller { get; private set; }
        public int Texture { get { return renderTargetHelper.RenderTexture; } }

        GaussianBlurShader gaussianBlur;
        FramebufferRenderTexture renderTargetHelper;
        FramebufferRenderTexture bgTargetHelper;
        Primitives primitives;
        WallpaperImageSettingsService wpSettings;
        private string lastBgPath = null;
        private bool bgDrawn = false;
        private bool dirty = false;

        public BackgroundRenderer(BackgroundController controller)
        {
            Controller = controller;
            primitives = ServiceManager.GetService<Primitives>();
            gaussianBlur = new GaussianBlurShader();
            RenderResolutionChanged();
            wpSettings = ServiceManager.GetService<WallpaperImageSettingsService>();
        }

        public void RenderResolutionChanged()
        {
            renderTargetHelper?.Cleanup();
            renderTargetHelper = null;
            renderTargetHelper = new FramebufferRenderTexture((int)Controller.RenderResolution.X, (int)Controller.RenderResolution.Y, framebufferHooks);

            bgTargetHelper?.Cleanup();
            bgTargetHelper = null;
            bgTargetHelper = new FramebufferRenderTexture((int)Controller.RenderResolution.X, (int)Controller.RenderResolution.Y, framebufferHooks);
            bgDrawn = false;
        }

        public void Render()
        {
            bool wereSettingsUpdatedRecently = (TimeManager.FrameNumber - wpSettings.LastFrameSettingsWereChanged) < 2; //settings gui renders after the background already rendered, so we check if settings were updated within a few frames. if they were, background is dirty.
            if (!dirty && !wereSettingsUpdatedRecently && !Background.Animated && lastBgPath == Background.SourcePath)
            {
                //static wallpaper, not just updated
                if (bgDrawn)
                {
                    //already rendered
                    //do not rerender
                    return;
                }
            }
            lastBgPath = Background.SourcePath;
            bgDrawn = true;
            var settingsForImage = wpSettings.GetImageSettingsForPath(Background.SourcePath);
            var currentMode = BackgroundMode.BorderedDefault;
            var backgroundColor = System.Numerics.Vector4.One;
            var align = BackgroundAnchorPosition.Center;
            gaussianBlur.Activate();
            gaussianBlur.SetResolution(Controller.RenderResolution);

            if (settingsForImage != null)
            {
                currentMode = settingsForImage.Mode;
                backgroundColor = settingsForImage.BackgroundColor;
                align = settingsForImage.AnchorPosition;
            }

            float aspect = ((Background.Resolution.Width / Background.Resolution.Height) / (Controller.RenderResolution.X / Controller.RenderResolution.Y));

            VertexFloatBuffer renderBuffer = new VertexFloatBuffer(VertexFormat.XY_UV, limit: 128, bufferHint: BufferUsageHint.StreamDraw);

            float normPixelX = 1f / Background.Resolution.Width;
            float normPixelY = 1f / Background.Resolution.Height;
            float imageWidth = Background.Resolution.Width;
            float imageHeight = Background.Resolution.Height;
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
                renderBuffer.AddVertex(0f, Background.Resolution.Height, 0f, 1f);
                renderBuffer.AddVertex(Background.Resolution.Width, Background.Resolution.Height, 1f, 1f);
                renderBuffer.AddVertex(0f, 0f, 0f, 0f);
                renderBuffer.AddVertex(Background.Resolution.Width, Background.Resolution.Height, 1f, 1f);
                renderBuffer.AddVertex(Background.Resolution.Width, 0f, 1f, 0f);
                renderBuffer.Load();
            }

            GL.PushAttrib(AttribMask.AllAttribBits);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Modelview);
            switch (currentMode)
            {
                case BackgroundMode.BorderedDefault:
                    {
                        gaussianBlur.SetBlurState(true);

                        float bgImageScalar = Math.Max(1f, 1f / aspect * 0.75f);

                        float horizontalMove = aspect;
                        bgTargetHelper.BindAndRenderTo(true);

                        //GL.BindTexture(TextureTarget.Texture2D, textureIndex);
                        Background.BindTexture();

                        GL.Scale(2, 2, 1);
                        {
                            GL.Enable(EnableCap.ScissorTest);
                            GL.Scissor(0, 0, (int)(Controller.RenderResolution.X * 0.5f), (int)Controller.RenderResolution.Y);
                            GL.PushMatrix();
                            GL.Translate(-horizontalMove, 0, 0);
                            GL.Scale(aspect, 1, 1);
                            GL.Scale(bgImageScalar, bgImageScalar, 1);
                            primitives.CenteredQuad.Draw();
                            GL.PopMatrix();
                        }
                        {
                            GL.Scissor((int)(Controller.RenderResolution.X * 0.5f), 0, (int)(Controller.RenderResolution.X * 0.5f), (int)Controller.RenderResolution.Y);
                            GL.PushMatrix();
                            GL.Translate(horizontalMove, 0, 0);
                            GL.Scale(aspect, 1, 1);
                            GL.Scale(bgImageScalar, bgImageScalar, 1);
                            primitives.CenteredQuad.Draw();
                            GL.PopMatrix();
                            GL.Disable(EnableCap.ScissorTest);
                        }


                        bgTargetHelper.FinishRendering();

                        renderTargetHelper.BindAndRenderTo(true);

                        GL.BindTexture(TextureTarget.Texture2D, bgTargetHelper.RenderTexture);

                        primitives.CenteredQuad.Draw();

                        Background.BindTexture();

                        gaussianBlur.SetBlurState(false);

                        {
                            //pixel-perfect rendering of center image
                            GL.PushMatrix();
                            GL.LoadIdentity();
                            GL.Ortho(0, Controller.RenderResolution.X, 0, Controller.RenderResolution.Y, -1, 1);
                            
                            if (Background.Resolution.Height != Controller.RenderResolution.Y)
                            {
                                double imgScalar = Controller.RenderResolution.Y / Background.Resolution.Height;
                                GL.Translate((int)((Controller.RenderResolution.X * 0.5f) - (imageWidth * 0.5f * imgScalar)), 0, 0);
                                GL.Scale(imgScalar, imgScalar, 1);
                            }
                            else
                            {
                                GL.Translate((int)((Controller.RenderResolution.X * 0.5f) - (imageWidth * 0.5f)), 0, 0);
                            }
                            renderBuffer.Draw();
                            GL.PopMatrix();
                        }

                        gaussianBlur.SetColorOverride(true, new Vector4(0, 0, 0, 1));
                        {
                            float targetSizeInPixels = 9f * (Controller.RenderResolution.X / 1920f);
                            float barWidth = (1f / Controller.RenderResolution.X) * targetSizeInPixels;

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
                        gaussianBlur.SetBlurState(false);

                        renderTargetHelper.BindAndRenderTo(true);

                        GL.Ortho(0, Controller.RenderResolution.X, 0, Controller.RenderResolution.Y, -1, 1);
                        double imgHeightCorrectScalar = Controller.RenderResolution.Y / Background.Resolution.Height;

                        if (settingsForImage.BackgroundStyle == SolidBackgroundStyle.SolidColor)
                        {
                            gaussianBlur.SetColorOverride(true, new Vector4(backgroundColor.X, backgroundColor.Y, backgroundColor.Z, backgroundColor.W));
                            GL.PushMatrix();
                            GL.Scale(Controller.RenderResolution.X, Controller.RenderResolution.Y, 1);
                            primitives.QuadBuffer.Draw();
                            GL.PopMatrix();
                        }
                        else if (settingsForImage.BackgroundStyle == SolidBackgroundStyle.StretchEdge)
                        {
                            Background.BindTexture();
                            gaussianBlur.SetColorOverride(false, Vector4.One);
                            //flip uv coordinates of left rect
                            float uvSampleWidth = settingsForImage.StretchWidth / Controller.RenderResolution.X;
                            float uvLeftSamplePos = (settingsForImage.StretchXPos / Controller.RenderResolution.X) + uvSampleWidth;
                            float uvRightSamplePos = 1f - (((settingsForImage.StretchXPos + settingsForImage.StretchWidth) / Controller.RenderResolution.X) - uvSampleWidth);

                            if (settingsForImage.AnchorPosition == BackgroundAnchorPosition.Center)
                            {
                                VertexFloatBuffer leftRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF(0, 0, (int)((Controller.RenderResolution.X * 0.5f) - (imageWidth * imgHeightCorrectScalar * 0.5f)), Controller.RenderResolution.Y),
                                    new RectangleF(uvLeftSamplePos, 0, -uvSampleWidth, 1));

                                leftRect.Draw();

                                VertexFloatBuffer rightRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF((int)((Controller.RenderResolution.X * 0.5f) + (imageWidth * imgHeightCorrectScalar * 0.5f)), 0, (int)((Controller.RenderResolution.X * 0.5f) - (imageWidth * imgHeightCorrectScalar * 0.5f)), Controller.RenderResolution.Y),
                                    new RectangleF(uvRightSamplePos, 0, -uvSampleWidth, 1));

                                rightRect.Draw();

                                rightRect.Unload();
                                leftRect.Unload();
                            }
                            else if (settingsForImage.AnchorPosition == BackgroundAnchorPosition.Left)
                            {
                                var rightRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF(imageWidth, 0, Controller.RenderResolution.X - imageWidth, Controller.RenderResolution.Y),
                                    new RectangleF(uvRightSamplePos, 0, -uvSampleWidth, 1));

                                rightRect.Draw();
                                rightRect.Unload();
                            }
                            else if (settingsForImage.AnchorPosition == BackgroundAnchorPosition.Right)
                            {
                                var leftRect = Primitives.GenerateXY_UVRect(
                                    new RectangleF(0, 0, Controller.RenderResolution.X - imageWidth, Controller.RenderResolution.Y),
                                    new RectangleF(uvLeftSamplePos, 0, -uvSampleWidth, 1));

                                leftRect.Draw();
                                leftRect.Unload();
                            }
                        }

                        gaussianBlur.SetColorOverride(false, Vector4.Zero);

                        Background.BindTexture();
                        {

                            switch (align)
                            {
                                case BackgroundAnchorPosition.Center:
                                    GL.Translate((int)((Controller.RenderResolution.X * 0.5f) - ((imageWidth * imgHeightCorrectScalar) * 0.5f)), 0, 0);
                                    break;

                                case BackgroundAnchorPosition.Right:
                                    GL.Translate((int)(Controller.RenderResolution.X - imageWidth), 0, 0);
                                    break;
                            }

                            if (Background.Resolution.Height != Controller.RenderResolution.Y)
                            {
                                GL.Scale(imgHeightCorrectScalar, imgHeightCorrectScalar, 1);
                            }

                            renderBuffer.Draw();
                        }

                        renderTargetHelper.FinishRendering();
                        break;
                    }
            }
            renderBuffer.Unload();
            gaussianBlur.SetColorOverride(false, Vector4.Zero);

            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);

            GL.PopMatrix();
            GL.PopAttrib();

            dirty = false;
        }
    }
}
