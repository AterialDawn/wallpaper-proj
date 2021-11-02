using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace player.Core.FFmpeg
{
    class FileNotFoundException : Exception
    {
        public string FilePath { get; private set; }

        public FileNotFoundException(string filePath)
            : base()
        {
            this.FilePath = filePath;
        }

        public FileNotFoundException(string filePath, string message)
            : base(message)
        {
            this.FilePath = filePath;
        }

        public FileNotFoundException(string filePath, string message, Exception innerException)
            : base(message, innerException)
        {
            this.FilePath = filePath;
        }
    }

    class InvalidFileException : Exception
    {
        public string FilePath { get; private set; }

        public InvalidFileException(string filePath)
            : base()
        {
            this.FilePath = filePath;
        }

        public InvalidFileException(string filePath, string message)
            : base(message)
        {
            this.FilePath = filePath;
        }

        public InvalidFileException(string filePath, string message, Exception innerException)
            : base(message, innerException)
        {
            this.FilePath = filePath;
        }
    }

    class FFmpegInitializationException : Exception
    {
        public FFmpegInitializationException() : base() { }

        public FFmpegInitializationException(string message) : base(message) { }

        public FFmpegInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
