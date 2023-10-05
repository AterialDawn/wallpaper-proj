using FFmpeg.AutoGen;
using System;

namespace player.Core.FFmpeg
{
    /// <summary>
    /// Contains information about a frame decoded with ffmpeg. Allocates unmanaged memory.
    /// </summary>
    abstract unsafe class BaseFFMpegFrameContainer : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public double PresentationTimeStamp { get; private set; }

        public AVFrame* Frame { get; private set; }

        private bool _disposed = false;

        public BaseFFMpegFrameContainer(int width, int height, double presentationTimeStamp, AVFrame* sourceFrame)
        {
            Width = width;
            Height = height;
            this.PresentationTimeStamp = presentationTimeStamp;
            Frame = sourceFrame;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _disposed = true;
                }
            }
        }
    }

    unsafe class FFMpegYUV420FrameContainer : BaseFFMpegFrameContainer
    {
        public IntPtr YFramePointer { get; private set; }
        public int YFrameSize { get; private set; }

        public IntPtr UFramePointer { get; private set; }
        public int UFrameSize { get; private set; }
        public IntPtr VFramePointer { get; private set; }
        public int VFrameSize { get; private set; }
        public FFMpegYUV420FrameContainer(int width, int height, double presentationTimeStamp, AVFrame* sourceFrame) : base(width, height, presentationTimeStamp, sourceFrame)
        {
            YFramePointer = (IntPtr)(*sourceFrame).data[0];
            UFramePointer = (IntPtr)(*sourceFrame).data[1];
            VFramePointer = (IntPtr)(*sourceFrame).data[2];

            YFrameSize = (*sourceFrame).linesize[0];
            UFrameSize = (*sourceFrame).linesize[1];
            VFrameSize = (*sourceFrame).linesize[2];
        }
    }

    unsafe class FFMpegYUV444FrameContainer : FFMpegYUV420FrameContainer
    {
        public FFMpegYUV444FrameContainer(int width, int height, double presentationTimeStamp, AVFrame* sourceFrame) : base(width, height, presentationTimeStamp, sourceFrame) { }
    }

    unsafe class FFMpegGBRPFrameContainer : BaseFFMpegFrameContainer
    {
        public IntPtr GreenFramePointer { get; private set; }
        public IntPtr BlueFramePointer { get; private set; }
        public IntPtr RedFramePointer { get; private set; }
        public int FrameSize { get; private set; }
        public int GBRFrameSize { get; private set; }
        public FFMpegGBRPFrameContainer(int width, int height, double presentationTimeStamp, AVFrame* sourceFrame) : base(width, height, presentationTimeStamp, sourceFrame)
        {
            GreenFramePointer = (IntPtr)(*sourceFrame).data[0];
            BlueFramePointer = (IntPtr)(*sourceFrame).data[1];
            RedFramePointer = (IntPtr)(*sourceFrame).data[2];
            GBRFrameSize = (*sourceFrame).linesize[0];
        }
    }
}