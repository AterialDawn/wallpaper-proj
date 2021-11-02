using System;
using player.Core.Service;
using FFmpeg.AutoGen;
using System.IO;
using System.Reflection;

namespace player.Core.FFmpeg
{
    class FFMpegManager : IService
    {
        #region Singleton
        private static FFMpegManager _instance = new FFMpegManager();
        public static FFMpegManager Instance { get { if (_instance == null)_instance = new FFMpegManager(); return _instance; } }

        public string ServiceName { get { return "FFMpegManager"; } }

        public FFMpegManager()
        {

        }

        public void Initialize()
        {
            ffmpeg.RootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ffbin_x64");
            Logging.Logger.Log($"FFMPEG path set to {ffmpeg.RootPath}");
        }

        public void Cleanup()
        {

        }

        #endregion

        private void LoadDummyDecoder()
        {
            FFMpegDecoder tempDecoder = new FFMpegDecoder("");
            tempDecoder.Dispose();
        }

        //this class will open any file that ffmpeg can open, verify that it has an image stream, and return a class that decodes that image stream into a specified format for use with ogl

        public FFMpegDecoder GetDecoder(string sourceFilePath)
        {
            if (Program.DisableFFmpeg) throw new InvalidOperationException("FFmpeg was disabled! Unable to decode!");
            return new FFMpegDecoder(sourceFilePath);
        }
    }
}
