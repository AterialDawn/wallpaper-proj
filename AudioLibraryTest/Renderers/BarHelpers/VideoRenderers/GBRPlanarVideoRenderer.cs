﻿using OpenTK.Graphics.OpenGL;
using player.Core.FFmpeg;
using player.Shaders;
using player.Utility;

namespace player.Renderers.BarHelpers.VideoRenderers
{
    class GBRPlanarVideoRenderer : BaseVideoRenderer
    {

        int[] greenTextures = new int[FRAME_BUFFER_SIZE];
        int[] blueTextures = new int[FRAME_BUFFER_SIZE];
        int[] redTextures = new int[FRAME_BUFFER_SIZE];

        GBRPlanarShader shader;

        public GBRPlanarVideoRenderer(FFMpegDecoder decoder, FFMpegGBRPFrameContainer frame) : base(decoder, frame.Width, frame.Height)
        {
            //we are in threadedloadercontext, still inside Preload handler, the framebuffer has been created already in base(), so we initialize our 3? textures
            GL.GenTextures(FRAME_BUFFER_SIZE, greenTextures);
            GL.GenTextures(FRAME_BUFFER_SIZE, blueTextures);
            GL.GenTextures(FRAME_BUFFER_SIZE, redTextures);

            shader = new GBRPlanarShader();
            for (int i = 0; i < FRAME_BUFFER_SIZE; i++)
            {
                TextureUtils.LoadPtrIntoTexture(frame.Width, frame.Height, greenTextures[i], OpenTK.Graphics.OpenGL4.PixelInternalFormat.R8, OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.GreenFramePointer);
                TextureUtils.LoadPtrIntoTexture(frame.Width, frame.Height, blueTextures[i], OpenTK.Graphics.OpenGL4.PixelInternalFormat.R8, OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.BlueFramePointer);
                TextureUtils.LoadPtrIntoTexture(frame.Width, frame.Height, redTextures[i], OpenTK.Graphics.OpenGL4.PixelInternalFormat.R8, OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.RedFramePointer);
            }

            decoder.ReleaseFrame(frame);
        }

        public override void Cleanup()
        {
            base.Cleanup();

            GL.DeleteTextures(FRAME_BUFFER_SIZE, greenTextures);
            GL.DeleteTextures(FRAME_BUFFER_SIZE, blueTextures);
            GL.DeleteTextures(FRAME_BUFFER_SIZE, redTextures);
        }

        public override void PreloadFrames()
        {
            for (int i = 1; i < FRAME_BUFFER_SIZE; i++)
            {
                var frame = Decoder.GetFrame<FFMpegGBRPFrameContainer>();

                TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, greenTextures[i], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.GreenFramePointer);
                TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, blueTextures[i], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.BlueFramePointer);
                TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, redTextures[i], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.RedFramePointer);

                Decoder.ReleaseFrame(frame);
            }
            currentWriteIndex = FRAME_BUFFER_SIZE - 1; //change write head to last element
        }

        public override void RenderNextFrame()
        {
            base.RenderNextFrame();

            shader.Activate();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, greenTextures[currentTextureIndex]);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, blueTextures[currentTextureIndex]);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, redTextures[currentTextureIndex]);

            Primitives.QuadBuffer.Draw();

            int framesToDecode = 1 + skippedFrames;

            while (framesToDecode-- > 0)
            {
                var frame = Decoder.GetFrame<FFMpegGBRPFrameContainer>();

                if (framesToDecode == 0) //only upload the non-skipped frame to opengl
                {
                    TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, greenTextures[currentWriteIndex], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.GreenFramePointer);
                    TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, blueTextures[currentWriteIndex], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.BlueFramePointer);
                    TextureUtils.UpdateTextureFromPtr(frame.Width, frame.Height, redTextures[currentWriteIndex], OpenTK.Graphics.OpenGL4.PixelFormat.Red, frame.RedFramePointer);

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
