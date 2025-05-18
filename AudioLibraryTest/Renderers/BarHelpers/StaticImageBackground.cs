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
using System.Drawing.Imaging;
using System.IO;

namespace player.Renderers.BarHelpers
{
    class StaticImageBackground : BackgroundBase
    {
        public override bool Animated { get { return false; } }
        public Size SourceImageSize { get; private set; } = Size.Empty;

        private int textureIndex;

        Bitmap image;
        WallpaperImageSettingsService wpSettings;

        public StaticImageBackground(string sourcePath)
        {
            SourcePath = sourcePath;
            wpSettings = ServiceManager.GetService<WallpaperImageSettingsService>();
        }

        public override bool Preload()
        {
            textureIndex = GL.GenTexture();
            try
            {
                uploadToGL();
                return true;
            }
            catch (Exception e)
            {
                ReportPreloadError($"Error loading : {Path.GetFileName(SourcePath)}", e);
            }
            return false;
        }

        public override void Update(double elapsedTime)
        {
            if (TimeManager.FrameNumber - wpSettings.LastFrameSettingsWereChanged < 2) uploadToGL();
        }

        public override int GetTextureIndex() => textureIndex;

        public override void Destroy()
        {
            GL.DeleteTexture(textureIndex);
            image.Dispose();
        }

        void uploadToGL()
        {
            if (image == null) image = new Bitmap(SourcePath);
            SourceImageSize = image.Size;
            var settingsForImage = wpSettings.GetImageSettingsForPath(SourcePath);
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

            if (Resolution.Width != RenderResolution.X || Resolution.Height != RenderResolution.Y)
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

                        if (settingsForImage.FlipMode.HasFlag(FlipMode.FlipX))
                        {
                            g.TranslateTransform((float)targetWidth / 2f, (float)RenderResolution.Y / 2f);
                            g.ScaleTransform(-1, 1);
                            g.TranslateTransform(-(float)targetWidth / 2f, -(float)RenderResolution.Y / 2f);
                        }
                        if (settingsForImage.FlipMode.HasFlag(FlipMode.FlipY))
                        {
                            g.TranslateTransform((float)targetWidth / 2f, (float)RenderResolution.Y / 2f);
                            g.ScaleTransform(1, -1);
                            g.TranslateTransform(-(float)targetWidth / 2f, -(float)RenderResolution.Y / 2f);
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
                }
            }
            else
            {
                TextureUtils.LoadBitmapIntoTexture(image, textureIndex);
            }


            GL.Finish();
        }
    }
}
