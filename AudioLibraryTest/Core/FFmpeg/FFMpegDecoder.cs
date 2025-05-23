﻿using FFmpeg.AutoGen;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Log = player.Core.Logging.Logger;

namespace player.Core.FFmpeg
{
    unsafe class FFMpegDecoder : IDisposable
    {
        public bool Decoding { get; private set; }
        public string FilePath { get; private set; }
        public double FrameDelay { get; private set; }
        public int FramesToBuffer { get; private set; }
        public int DecodedFrameCount { get; private set; }
        public string CodecName { get; private set; } = "";
        public AVPixelFormat PixelFormat { get; private set; } = AVPixelFormat.AV_PIX_FMT_NONE;
        public AVColorRange ColorRange { get; private set; } = AVColorRange.AVCOL_RANGE_UNSPECIFIED;
        public TimeSpan VideoLength { get; private set; } = TimeSpan.Zero;

        private ManualResetEventSlim framesDecodedEvent = new ManualResetEventSlim(false);

        private int width = 0, height = 0;

        private AVFormatContext* _pFormatContext;
        private AVCodecContext* _pCodecContext;
        private AVFrame*[] _frameList = new AVFrame*[3];
        private BlockingCollection<FrameContainer> _frameCollection = new BlockingCollection<FrameContainer>();
        private BlockingCollection<BaseFFMpegFrameContainer> _availableFrames = new BlockingCollection<BaseFFMpegFrameContainer>();
        private AVPacket* _pPacket;
        private int _streamIndex;
        private double timeBase;

        private Thread decoderThread = null;


        public FFMpegDecoder(string sourceFilePath)
        {
            this.FilePath = sourceFilePath;
            this.DecodedFrameCount = 0;
            this.FramesToBuffer = 0;
            this.Decoding = false;
        }

        public bool WaitUntilFramesDecoded(int waitDelay = -1)
        {
            return framesDecodedEvent.Wait(waitDelay);
        }

        public T GetFrame<T>() where T : BaseFFMpegFrameContainer
        {
            return _availableFrames.Take() as T;
        }

        public void ReleaseFrame(BaseFFMpegFrameContainer frame)
        {
            _frameCollection.Add(new FrameContainer { _frame = frame.Frame });
        }

        /// <summary>
        /// Starts the decoding process. Will throw exceptions on invalid files, or ffmpeg initialization errors.
        /// FramesDecoded will be set once a single frame has been decoded, once this event is fired, it is safe to process frames from FrameBuffer
        /// </summary>
        public unsafe void StartDecoding()
        {
            if (Decoding) return;
            Decoding = true;
            decoderThread = new Thread(DecodeProc);
            decoderThread.IsBackground = true;
            decoderThread.Name = "FFMPEG Decoder Thread";
            decoderThread.Start();
        }

        public void StopDecoding()
        {
            this.Decoding = false;
            Log.Log("Stop Decoding");
            _frameCollection.CompleteAdding();
            _availableFrames.CompleteAdding();

            //Flush Buffers
            while (_frameCollection.TryTake(out var _)) ;
            while (_availableFrames.TryTake(out var _)) ;
        }

        private void DecodeProc(object _)
        {
            _pFormatContext = ffmpeg.avformat_alloc_context();
            var pFormatContext = _pFormatContext;

            ffmpeg.avformat_open_input(&pFormatContext, FilePath, null, null);
            ffmpeg.avformat_find_stream_info(pFormatContext, null);

            AVCodec* pCodec = null;
            _streamIndex = ffmpeg.av_find_best_stream(_pFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &pCodec, 0);
            _pCodecContext = ffmpeg.avcodec_alloc_context3(pCodec);
            int threads = Environment.ProcessorCount / 2;
            if (threads > 4) threads = 4; //no more than 4 threads even when more than 4 are available
            _pCodecContext->thread_count = threads;
            _pCodecContext->thread_type = ffmpeg.FF_THREAD_SLICE;

            ffmpeg.avcodec_parameters_to_context(_pCodecContext, pFormatContext->streams[_streamIndex]->codecpar);
            ffmpeg.avcodec_open2(_pCodecContext, pCodec, null);
            CodecName = ffmpeg.avcodec_get_name(pCodec->id);

            width = _pCodecContext->width;
            height = _pCodecContext->height;
            PixelFormat = _pCodecContext->pix_fmt;
            ColorRange = _pCodecContext->color_range;
            CheckForValidPixelFormats();
            FrameDelay = (double)_pFormatContext->streams[_streamIndex]->avg_frame_rate.den / (double)_pFormatContext->streams[_streamIndex]->avg_frame_rate.num;

            for (int i = 0; i < _frameList.Length; i++)
            {
                _frameList[i] = ffmpeg.av_frame_alloc();
                _frameCollection.Add(new FrameContainer { _frame = _frameList[i] });
            }
            _pPacket = ffmpeg.av_packet_alloc();

            timeBase = ffmpeg.av_q2d(_pFormatContext->streams[_streamIndex]->time_base);

            double duration = (double)pFormatContext->streams[_streamIndex]->duration * timeBase;
            if (duration < 0) //LOL WTF
            {
                duration = pFormatContext->duration * timeBase / 1000.0; //unsure if /1000 is correct, pFormatContext seems to be in ms, but the stream itself seems to be in whole seconds?
            }
            VideoLength = TimeSpan.FromSeconds(duration);

            while (Decoding)
            {
                bool decodeResult = false;
                AVFrame* frame = null;
                try
                {
                    decodeResult = TryDecodeNextFrame(out frame);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                if (decodeResult)
                {
                    try
                    {
                        AddFrameToBuffer(frame);
                    }
                    catch (InvalidOperationException)
                    {
                        break;
                    }
                    framesDecodedEvent.Set();
                }
                else
                {
                    _frameCollection.Add(new FrameContainer { _frame = frame }); //restore frame to buffer
                    ffmpeg.avformat_seek_file(pFormatContext, _streamIndex, 0, 0, 0, 0);
                    ffmpeg.avcodec_flush_buffers(_pCodecContext);
                    Log.Log("Looping Video");
                    //reset decoder if we should loop, else break out and signal completion
                }
            }

            foreach (var _pFrame in _frameList)
            {
                ffmpeg.av_frame_unref(_pFrame);
                var frameP = _pFrame;
                ffmpeg.av_frame_free(&frameP);
            }

            ffmpeg.av_packet_unref(_pPacket);
            ffmpeg.av_free(_pPacket);

            ffmpeg.avcodec_close(_pCodecContext);
            ffmpeg.avformat_close_input(&pFormatContext);

            Log.Log("Decode loop finished, freed everything");
        }

        void CheckForValidPixelFormats()
        {
            switch (PixelFormat)
            {
                case AVPixelFormat.AV_PIX_FMT_YUV420P:
                case AVPixelFormat.AV_PIX_FMT_GBR24P:
                case AVPixelFormat.AV_PIX_FMT_YUV444P:
                    break;
                default:
                    throw new InvalidOperationException($"Pixel Format {PixelFormat} not supported!");
            }
        }

        private void AddFrameToBuffer(AVFrame* frame)
        {
            double timeStamp = (*frame).pts * timeBase;
            switch (PixelFormat)
            {
                case AVPixelFormat.AV_PIX_FMT_GBR24P:
                    {
                        _availableFrames.Add(new FFMpegGBRPFrameContainer(width, height, timeStamp, frame));
                        break;
                    }
                case AVPixelFormat.AV_PIX_FMT_YUV420P:
                    {
                        _availableFrames.Add(new FFMpegYUV420FrameContainer(width, height, timeStamp, frame));
                        break;
                    }
                case AVPixelFormat.AV_PIX_FMT_YUV444P:
                    {
                        _availableFrames.Add(new FFMpegYUV444FrameContainer(width, height, timeStamp, frame));
                        break;
                    }
                default:
                    {
                        Log.Log($"Attempted to add a frame of unsupported pixel format {PixelFormat}");
                        break;
                    }
            }
        }

        private bool TryDecodeNextFrame(out AVFrame* frame)
        {
            var _pFrame = _frameCollection.Take()._frame;
            ffmpeg.av_frame_unref(_pFrame);
            int error;
            do
            {
                try
                {
                    do
                    {
                        error = ffmpeg.av_read_frame(_pFormatContext, _pPacket);
                        if (error == ffmpeg.AVERROR_EOF)
                        {
                            frame = _pFrame;
                            return false;
                        }

                        error.ThrowExceptionIfError(this);
                    } while (_pPacket->stream_index != _streamIndex);

                    ffmpeg.avcodec_send_packet(_pCodecContext, _pPacket).ThrowExceptionIfError(this);
                }
                finally
                {
                    ffmpeg.av_packet_unref(_pPacket);
                }

                error = ffmpeg.avcodec_receive_frame(_pCodecContext, _pFrame);
            } while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN));
            error.ThrowExceptionIfError(this);
            frame = _pFrame;
            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        unsafe class FrameContainer
        {
            public AVFrame* _frame;
        }
    }
}