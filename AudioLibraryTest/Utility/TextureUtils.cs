using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace player.Utility
{
    static class TextureUtils
    {
        public static void LoadBitmapIntoTexture(Bitmap sourceBitmap, int oglTexture)
        {
            BitmapData TextureData = sourceBitmap.LockBits(
                new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

            LoadPtrIntoTexture(sourceBitmap.Width, sourceBitmap.Height, oglTexture, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, TextureData.Scan0);

            sourceBitmap.UnlockBits(TextureData);
        }

        public static void LoadPtrIntoTexture(int width, int height, int oglTexture, OpenTK.Graphics.OpenGL4.PixelFormat pixelFormat, IntPtr ptr, int stride = 0)
        {
            if(stride != 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, stride);

            GL.BindTexture(TextureTarget.Texture2D, oglTexture);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, pixelFormat, PixelType.UnsignedByte, ptr);

            if (stride != 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        }

        public static void LoadPtrIntoTexture(int width, int height, int oglTexture, PixelInternalFormat internalFormat, OpenTK.Graphics.OpenGL4.PixelFormat pixelFormat, IntPtr ptr, int stride = 0)
        {
            if (stride != 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, stride);

            GL.BindTexture(TextureTarget.Texture2D, oglTexture);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);

            GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, PixelType.UnsignedByte, ptr);

            if (stride != 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        }

        public static void UpdateTextureFromPtr(int width, int height, int oglTexture, OpenTK.Graphics.OpenGL4.PixelFormat pixelFormat, IntPtr ptr, int stride = 0, int xOffset = 0, int yOffset = 0)
        {
            if(stride != 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, stride);

            GL.BindTexture(TextureTarget.Texture2D, oglTexture);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, xOffset, yOffset, width, height, pixelFormat, PixelType.UnsignedByte, ptr);

            if(stride != 0) GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        }

        public static Bitmap LoadFlippedBitmapFromPath(string path)
        {
            Bitmap bitmap = (Bitmap)Bitmap.FromFile(path);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return bitmap;
        }
    }
}
