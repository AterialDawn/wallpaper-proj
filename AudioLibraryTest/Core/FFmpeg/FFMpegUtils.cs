﻿using FFmpeg.AutoGen;
using System;
using System.Runtime.InteropServices;

namespace player.Core.FFmpeg
{
    static class FFMpegUtils
    {
        public static int ThrowExceptionIfError(this int error, FFMpegDecoder context)
        {
            if (error < 0) throw new ApplicationException($"{av_strerror(error)}\nFilePath : {context.FilePath}");
            return error;
        }

        public static unsafe string av_strerror(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }
    }
}
