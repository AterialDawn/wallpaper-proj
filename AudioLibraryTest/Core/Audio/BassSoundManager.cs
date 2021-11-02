using System;
using System.Threading.Tasks;
using player.Utility;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Flac;
using player.Core.Service;
using player.Core.Input;

namespace player.Core.Audio
{
    public class BassSoundManager : IService, IBassSoundProvider
    {
        public string ServiceName { get { return "BassSoundManager"; } }

        internal BassSoundManager()
        {
            SongEndProc = new SYNCPROC(EndOfStreamProc);
        }

        private int EosHandle = 0;
        //private STREAMPROC UpdateProc;
        private SYNCPROC SongEndProc;
        private int PlayStream = 0;
        private string LastSong;
        private int Volume = 100;
        private int BassDevice = 0;
        private volatile bool loadingSong = false;

        #region IInitializable
        public void Initialize()
        {
            if (Program.DisableAudioProcessing) return;
            if (!Bass.BASS_Init(-1, 48000, BASSInit.BASS_DEVICE_DEFAULT | BASSInit.BASS_DEVICE_CPSPEAKERS, IntPtr.Zero)) throw new ApplicationException("Unable to initialize BASS library!");
            BassDevice = Bass.BASS_GetDevice(); //Cache device for future threadpool threads
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 2000);
#if BUILD_32BIT
            if (!BassFlac.LoadMe("x86"))
#else
            if(!BassFlac.LoadMe("x64"))
#endif
            {
                throw new System.IO.FileNotFoundException("Cannot initialize BassFlac", "bassflac.dll");
            }

            ServiceManager.GetService<InputManager>().MouseWheelEvent += BassSoundManager_MouseWheelEvent;
        }

        private void BassSoundManager_MouseWheelEvent(object sender, OpenTK.Input.MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                SetVolume(UtilityMethods.Clamp(GetVolume() + 5, 0, 100));
            }
            else if (e.Delta < 0)
            {
                SetVolume(UtilityMethods.Clamp(GetVolume() - 5, 0, 100));
            }
        }

        public void Cleanup()
        {
            FreeBass();
        }
        #endregion

        #region IBassSoundProvider
        float IBassSoundProvider.GetSoundLevel()
        {
            return GetChannelVolume();
        }

        void IBassSoundProvider.GetFFTData(ref float[] FFTData, int Length)
        {
            GetChannelFFTData(ref FFTData, Length);
        }

        void IBassSoundProvider.GetPCMData(ref float[] PCMData, int Length)
        {
            GetChannelPCMData(ref PCMData, Length);
        }

        float IBassSoundProvider.GetVolumeBoost()
        {
            return 1.5f;
        }
        #endregion

        /// <summary>
        /// Loads the song specified in SongPath in a worker thread, and begins playback when it's loaded
        /// </summary>
        /// <param name="SongPath">The song to load, will not load if file does not exist</param>
        /// <returns>If a previous call to SetCurrentSong is still executing, returns false, else true</returns>
        public bool SetCurrentSong(string SongPath)
        {
            if (loadingSong) return false;
            loadingSong = true;
            Task.Factory.StartNew(() =>
                {
                    bool IsFlac = SongPath.ToLower().EndsWith(".flac");
                    if (IsFlac)
                    {
                        PlayStream = BassFlac.BASS_FLAC_StreamCreateFile(SongPath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);
                    }
                    else
                    {
                        PlayStream = Bass.BASS_StreamCreateFile(SongPath, 0, 0, BASSFlag.BASS_DEFAULT);
                    }

                    EosHandle = Bass.BASS_ChannelSetSync(PlayStream, BASSSync.BASS_SYNC_POS, GetChannelLength(), SongEndProc, IntPtr.Zero);
                    LastSong = SongPath;
                    SetVolume(Volume);
                    PlayCurrentStream(true);
                    loadingSong = false;
                });
            return true;
        }

        public void SetChannelAttribute(BASSAttribute attrib, float value)
        {
            Bass.BASS_ChannelSetAttribute(PlayStream, attrib, value);
        }

        public float GetChannelVolume()
        {
            if (PlayStream == 0) return 0f;
            if (IsStreamPaused()) return 0f;
            int TotalVol = Bass.BASS_ChannelGetLevel(PlayStream);
            int Left = Utils.LowWord32(TotalVol);
            int Right = Utils.HighWord32(TotalVol);
            int Average = (Left + Right) / 2;
            return (float)Average / 32768f;
        }

        public void SetVolume(int NewVol, bool nonLinear = false)
        {
            Volume = NewVol;
            if (nonLinear)
            {
                double trueVolume = UtilityMethods.Clamp(Math.Sin(((double)Volume / 100f) * Math.PI / 2.0), 0.0, 1.0);
                SetChannelAttribute(BASSAttribute.BASS_ATTRIB_VOL, (float)trueVolume);
            }
            else
            {
                SetChannelAttribute(BASSAttribute.BASS_ATTRIB_VOL, (float)NewVol / 100f);
            }
        }

        public int GetVolume()
        {
            return Volume;
        }

        public void SetChannelSync(BASSSync sync,long param ,ref SYNCPROC sProc)
        {
            Bass.BASS_ChannelSetSync(PlayStream, sync, param, sProc, IntPtr.Zero);
        }

        public long GetChannelPosition()
        {
            return Bass.BASS_ChannelGetPosition(PlayStream);
        }

        public double GetChannelBytesToSeconds(long Position)
        {
            return Bass.BASS_ChannelBytes2Seconds(PlayStream, Position);
        }

        public long GetChannelSecondsToBytes(double Seconds)
        {
            return Bass.BASS_ChannelSeconds2Bytes(PlayStream, Seconds);
        }

        public float GetChannelPlayPercentage()
        {
            if (PlayStream == 0) return 0f;
            return UtilityMethods.Clamp((float)((double)GetChannelPosition() / (double)GetChannelLength()), 0f, 1f);
        }

        public long GetChannelLength()
        {
            return Bass.BASS_ChannelGetLength(PlayStream, BASSMode.BASS_POS_BYTES);
        }

        public void GetChannelFFTData(ref float[] FFTData, int Length)
        {
            Bass.BASS_ChannelGetData(PlayStream, FFTData, Length);
        }

        public void GetChannelPCMData(ref float[] OscilloscpeData, int Length)
        {
            Bass.BASS_ChannelGetData(PlayStream, OscilloscpeData, Length);
        }

        public void GetChannelAttribute(BASSAttribute attrib, ref float Value)
        {
            Bass.BASS_ChannelGetAttribute(PlayStream, attrib, ref Value);
        }

        public long GetStreamFilePosition(BASSStreamFilePosition mode)
        {
            return Bass.BASS_StreamGetFilePosition(PlayStream, mode);
        }

        public bool SetChannelPosition(long position)
        {
            return Bass.BASS_ChannelSetPosition(PlayStream, position);
        }

        public bool ChannelSlideAttribute(BASSAttribute attrib, float value, int time)
        {
            return Bass.BASS_ChannelSlideAttribute(PlayStream, attrib, value, time);
        }

        public bool ChannelIsSliding(BASSAttribute attrib)
        {
            return Bass.BASS_ChannelIsSliding(PlayStream, attrib);
        }

        public Int32 GetChannelLevel()
        {
            return Bass.BASS_ChannelGetLevel(PlayStream);
        }

        public void PauseCurrentStream()
        {
            Bass.BASS_ChannelPause(PlayStream);
        }

        public bool IsStreamPaused()
        {
            return Bass.BASS_ChannelIsActive(PlayStream) == BASSActive.BASS_ACTIVE_PAUSED;
        }

        public void PlayCurrentStream(bool restart = false)
        {
            Bass.BASS_ChannelPlay(PlayStream, restart);
        }

        public int GetActiveStream()
        {
            return PlayStream;
        }

        public BASSActive GetStreamActive()
        {
            return Bass.BASS_ChannelIsActive(PlayStream);
        }

        public void StopStream()
        {
            Bass.BASS_ChannelStop(PlayStream);
        }

        public void FreeBass()
        {
            if (PlayStream != 0)
            {
                if (GetStreamActive() != BASSActive.BASS_ACTIVE_STOPPED)
                    StopStream();
            }
            Bass.BASS_Free();
        }

        private void EndOfStreamProc(int handle, int channel, int data, IntPtr user)
        {
            Bass.BASS_ChannelPlay(PlayStream, true);
        }
    }
}
