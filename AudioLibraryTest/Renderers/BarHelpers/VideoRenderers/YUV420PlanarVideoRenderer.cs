﻿using OpenTK.Graphics.OpenGL;
using player.Core.FFmpeg;
using player.Shaders;
using player.Utility;

namespace player.Renderers.BarHelpers.VideoRenderers
{
    class YUV420PlanarVideoRenderer : BaseVideoRenderer
    {
        int[] yTex = new int[FRAME_BUFFER_SIZE];
        int[] uTex = new int[FRAME_BUFFER_SIZE];
        int[] vTex = new int[FRAME_BUFFER_SIZE];

        YUVPlanarShader shader;

        public YUV420PlanarVideoRenderer(FFMpegDecoder decoder, FFMpegYUV420FrameContainer frame) : base(decoder, frame.Width, frame.Height)
        {
            //we are in threadedloadercontext, still inside Preload handler, the framebuffer has been created already in base(), so we initialize our 3? textures
            GL.GenTextures(FRAME_BUFFER_SIZE, yTex);
            GL.GenTextures(FRAME_BUFFER_SIZE, uTex);
            GL.GenTextures(FRAME_BUFFER_SIZE, vTex);

            shader = new YUVPlanarShader();
            if (decoder.ColorRange == FFmpeg.AutoGen.AVColorRange.AVCOL_RANGE_MPEG) shader.LimitedToFullColorRangeConvert = true;
            for (int i = 0; i < FRAME_BUFFER_SIZE; i++)
            {
                TextureUtils.LoadPtrIntoTexture(frame.Width, frame.Height, yTex[i], OpenTK.Graphics.OpenGL4.PixelInternalFormat.R8, OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.YFramePointer, frame.YFrameSize);
                TextureUtils.LoadPtrIntoTexture(frame.Width / 2, frame.Height / 2, uTex[i], OpenTK.Graphics.OpenGL4.PixelInternalFormat.R8, OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.UFramePointer, frame.UFrameSize);
                TextureUtils.LoadPtrIntoTexture(frame.Width / 2, frame.Height / 2, vTex[i], OpenTK.Graphics.OpenGL4.PixelInternalFormat.R8, OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.VFramePointer, frame.VFrameSize);
            }

            decoder.ReleaseFrame(frame);
        }

        public override void Cleanup()
        {
            base.Cleanup();

            GL.DeleteTextures(FRAME_BUFFER_SIZE, yTex);
            GL.DeleteTextures(FRAME_BUFFER_SIZE, uTex);
            GL.DeleteTextures(FRAME_BUFFER_SIZE, vTex);
        }

        public override void PreloadFrames()
        {
            for (int i = 1; i < FRAME_BUFFER_SIZE; i++)
            {
                var frame = Decoder.GetFrame<FFMpegYUV420FrameContainer>();

                TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, yTex[i], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.YFramePointer, frame.YFrameSize);
                TextureUtils.UpdateTextureFromPtr(frame.Width / 2, frame.Height / 2, uTex[i], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.UFramePointer, frame.UFrameSize);
                TextureUtils.UpdateTextureFromPtr(frame.Width / 2, frame.Height / 2, vTex[i], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.VFramePointer, frame.VFrameSize);

                Decoder.ReleaseFrame(frame);
            }
            currentWriteIndex = FRAME_BUFFER_SIZE - 1; //change write head to last element
        }

        public override void RenderNextFrame()
        {
            base.RenderNextFrame();

            shader.Activate();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, yTex[currentTextureIndex]);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, uTex[currentTextureIndex]);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, vTex[currentTextureIndex]);

            Primitives.QuadBuffer.Draw();


            int framesToDecode = 1 + skippedFrames;

            while (framesToDecode-- > 0)
            {
                var frame = Decoder.GetFrame<FFMpegYUV420FrameContainer>();
                if (framesToDecode == 0)
                {
                    TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, yTex[currentWriteIndex], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.YFramePointer, frame.YFrameSize);
                    TextureUtils.UpdateTextureFromPtr(frame.Width / 2, frame.Height / 2, uTex[currentWriteIndex], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.UFramePointer, frame.UFrameSize);
                    TextureUtils.UpdateTextureFromPtr(frame.Width / 2, frame.Height / 2, vTex[currentWriteIndex], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.VFramePointer, frame.VFrameSize);

                    currentTextureIndex++;
                    if (currentTextureIndex >= FRAME_BUFFER_SIZE) currentTextureIndex = 0;
                    currentWriteIndex++;
                    if (currentWriteIndex >= FRAME_BUFFER_SIZE) currentWriteIndex = 0;
                }
                Decoder.ReleaseFrame(frame);
            }


        }
    }
}
