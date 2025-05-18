using OpenTK.Graphics.OpenGL4;
using player.Core.Render;
using player.Utility;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace player.Renderers.BarHelpers
{
    class AnimatedImageBackground : BackgroundBase
    {
        public override bool Animated { get { return true; } }

        public bool LoadAllFramesToMemory { get; set; } = false;

        private RotatingBuffer<int> indexBuffer;
        int frameCount = 0;
        int frameIndex = 0;
        double timeLeftForFrame = 0;
        int[] textureIndices;
        double[] frameDurations;
        private Bitmap gifBmp;
        private bool singleFrame = false;

        private const int FRAMEDELAYPROPERTY = 0x5100;

        public AnimatedImageBackground(string sourcePath)
        {
            SourcePath = sourcePath;
        }

        public override bool Preload()
        {
            try
            {
                gifBmp = (Bitmap)Bitmap.FromFile(SourcePath);

                Resolution = new SizeF(gifBmp.Width, gifBmp.Height);

                InitializeGifData();

                if (LoadAllFramesToMemory)
                {
                    LoadGifCompletely();
                }
                else
                {
                    textureIndices = new int[2];
                    GL.GenTextures(textureIndices.Length, textureIndices);
                    indexBuffer = new RotatingBuffer<int>(textureIndices.Length);
                    indexBuffer.Set(textureIndices);

                    //Load first frame of the gif into a texture
                    InitializeTextures();
                    timeLeftForFrame = frameDurations[0];
                }

                Dirty = true;
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
            Dirty = false;
            if (singleFrame) return;
            timeLeftForFrame -= elapsedTime;
            if (timeLeftForFrame < 0)
            {
                while (timeLeftForFrame < 0)
                {
                    frameIndex = (frameIndex + 1) % frameCount;
                    timeLeftForFrame += frameDurations[frameIndex];
                    if (LoadAllFramesToMemory)
                    {
                        indexBuffer.RotateElements();
                    }
                }
                if(!LoadAllFramesToMemory)
                {
                    ThreadedLoaderContext.Instance.ExecuteOnLoaderThread(LoadFrameIntoTexture);
                }
            }
        }

        public override int GetTextureIndex() => indexBuffer.GetCurrent();

        public override void Destroy()
        {
            GL.DeleteTextures(textureIndices.Length, textureIndices);

            Task.Run(() =>
            {
                gifBmp?.Dispose();
            });
        }

        private void InitializeGifData()
        {
            //Calculate frame timings
            PropertyItem gifTimings = gifBmp.GetPropertyItem(FRAMEDELAYPROPERTY);
            frameDurations = new double[gifTimings.Len / 4];
            for (int i = 0; i < frameDurations.Length; i++)
            {
                frameDurations[i] = (double)BitConverter.ToInt32(gifTimings.Value, i * 4) / 100.0;
            }

            frameCount = gifBmp.GetFrameCount(FrameDimension.Time);
            singleFrame = frameCount == 1;
        }

        private void LoadGifCompletely()
        {
            textureIndices = new int[frameDurations.Length];
            GL.GenTextures(textureIndices.Length, textureIndices);
            indexBuffer = new RotatingBuffer<int>(textureIndices.Length);
            indexBuffer.Set(textureIndices);

            //load all gif frames into gpu textures
            indexBuffer.Index = frameDurations.Length - 1; //set to last element, since LoadFrame uses GetNext. stupid but then i can reuse LoadFrame :)
            for (frameIndex = 0; frameIndex < frameDurations.Length; frameIndex++)
            {
                LoadFrameIntoTexture();
            }
        }

        private void InitializeTextures()
        {
            {
                gifBmp.SelectActiveFrame(FrameDimension.Time, 0);
                BitmapData lockedData = gifBmp.LockBits(new Rectangle(0, 0, gifBmp.Width, gifBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                TextureUtils.LoadPtrIntoTexture(gifBmp.Width, gifBmp.Height, indexBuffer.GetCurrent(), OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, lockedData.Scan0);

                gifBmp.UnlockBits(lockedData);

                GL.Finish();
            }

            if (!singleFrame)
            {
                gifBmp.SelectActiveFrame(FrameDimension.Time, 1);
                BitmapData lockedData = gifBmp.LockBits(new Rectangle(0, 0, gifBmp.Width, gifBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

                TextureUtils.LoadPtrIntoTexture(gifBmp.Width, gifBmp.Height, indexBuffer.GetNext(), OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, lockedData.Scan0);

                gifBmp.UnlockBits(lockedData);

                GL.Finish();
            }
        }

        private void LoadFrameIntoTexture()
        {
            gifBmp.SelectActiveFrame(FrameDimension.Time, frameIndex);
            BitmapData lockedData = gifBmp.LockBits(new Rectangle(0, 0, gifBmp.Width, gifBmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            if (LoadAllFramesToMemory)
            {
                TextureUtils.LoadPtrIntoTexture(gifBmp.Width, gifBmp.Height, indexBuffer.GetNext(), OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, lockedData.Scan0);
            }
            else
            {
                TextureUtils.UpdateTextureFromPtr(gifBmp.Width, gifBmp.Height, indexBuffer.GetNext(), OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, lockedData.Scan0);
            }

            gifBmp.UnlockBits(lockedData);

            GL.Finish();

            indexBuffer.RotateElements();
            Dirty = true;
        }
    }
}
