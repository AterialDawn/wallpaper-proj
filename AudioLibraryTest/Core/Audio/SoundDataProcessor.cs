//#define FOOBARSCALING

using System;
using System.Collections.Generic;
using System.Threading;
using player.Core.Render;
using player.Utility;
using Un4seen.Bass;
using player.Core.Service;
using Aterial.Utility;
using player.Core.Settings;

namespace player.Core.Audio
{
    public enum SoundDataTypes
    {
        BarData = 0,
        StereoOscilloscope
    }
    //i really should rewrite this. (03-31-06)
    public class SoundDataProcessor : IService
    {
        public const int OscilloscopeSamples = 2400;
        public const int BarCount = 1000;
        public static float SmoothingFactor { get { return smoothingFactorSetting.Get(); } set { smoothingFactorSetting.Set(UtilityMethods.Clamp(value, 0, 1)); } }
        static SettingsAccessor<float> smoothingFactorSetting;
        public int SampleFrequency = 48000;
        
        private BASSData maxFFT = (BASSData.BASS_DATA_FFT8192);
        float[] fftData = new float[4096];
        
        private float OscilloscopeDataLength = 1000;
        private int[] logIndex = new int[BarCount];
        private float[] freqVolScalar = new float[BarCount];

        public float[] BarValues { get { return barValuesBuffer.GetCurrent(); } }

        public string ServiceName { get { return "SoundDataProcessor"; } }

        private RotatingBuffer<float[]> barValuesBuffer = new RotatingBuffer<float[]>(2);

        private float[] streamData = new float[(1000 * 160) + 2];
        public float[,] OscilloscopeValues = new float[2, 2400];
        private bool IsClosing = false;
        private IBassSoundProvider soundProvider;
        private Thread UpdateThread;
        private float volumeBoost;

        public float SongBeat = 0;
        public float AveragedVolume = 0;

        private ManualResetEventSlim soundProviderSet = new ManualResetEventSlim(false);
        internal FpsLimitHelper FpsLimiter = new FpsLimitHelper(60, 60, 60, false);

        private delegate void UpdateDataDelegate();
        private List<UpdateDataDelegate> DataProcessors = new List<UpdateDataDelegate>();
        private Action<float[]> dataPostProcessDelegate = null;
        int DataProcessorIndex = 0;
        
        int[] frequencyMap;

        internal SoundDataProcessor()
        {
            smoothingFactorSetting = ServiceManager.GetService<SettingsService>().GetAccessor<float>(SettingsKeys.SDP_Smoothing, 0.85f);
            List<float[]> fillerList = new List<float[]>();
            for(int i = 0; i < barValuesBuffer.Count; i++)
            {
                fillerList.Add(new float[BarCount]);
            }
            barValuesBuffer.Set(fillerList.ToArray());

            BuildFrequencyMap();
            BuildLookupTables();
            DataProcessors.Add(GetBarData);
            DataProcessors.Add(GetStereoOscilloscopeData);
        }

        public void SetDataProcessor(SoundDataTypes e)
        {
            DataProcessorIndex = (int)e;
        }

        public void SetSoundProviderSource(IBassSoundProvider bassSoundProvider)
        {
            soundProvider = bassSoundProvider;
            volumeBoost = soundProvider.GetVolumeBoost();

            soundProviderSet.Set();
        }

        public void SetDataPostProcessDelegate(Action<float[]> newDelegate)
        {
            dataPostProcessDelegate = newDelegate;
        }

        public Action<float[]> GetDataPostProcessDelegate()
        {
            return dataPostProcessDelegate;
        }

#if FOOBARSCALING
        internal void BuildLookupTables()
        {
            freqVolScalar[0] = 1f;
            logIndex[0] = Utils.FFTFrequency2Index(frequencyMap[0], 8192, SampleFrequency);

            for (int i = 1; i < BarCount; i++)
            {
                float sampleLocation = (float)i / (float)BarCount;
                float nextSampleLoc = (i + 1 >= BarCount) ? sampleLocation : ((float)i + 1f) / (float)BarCount;
                float mapSampleLoc = (sampleLocation * ((float)frequencyMap.Length - 1f));

                float mixFactor = mapSampleLoc - (float)Math.Floor(mapSampleLoc);

                int lowSample = (int)mapSampleLoc;
                int lowFreq = frequencyMap[lowSample];
                int highFreq = frequencyMap[lowSample + 1 >= frequencyMap.Length ? lowSample : lowSample + 1];
                
                int finalFreq = (int)Math.Floor(UtilityMethods.LinearInterpolate(lowFreq, highFreq, mixFactor));

                logIndex[i] = Utils.FFTFrequency2Index(finalFreq, 8192, SampleFrequency);
                freqVolScalar[i] = 1 + (float)Math.Sqrt((double)i / (double)BarCount) * 2f;
            }
        }
#else

        internal void BuildLookupTables()
        {
            int MaximumFrequency = 20500;
            int MinimumFrequency = 0;
            int maximumFrequencyIndex;
            int minimumFrequencyIndex;

            maximumFrequencyIndex = Math.Min(Utils.FFTFrequency2Index(MaximumFrequency, 8192, SampleFrequency) + 1, 4095);
            minimumFrequencyIndex = Math.Min(Utils.FFTFrequency2Index(MinimumFrequency, 8192, SampleFrequency), 4095);

            freqVolScalar[0] = 1f;
            for (int i = 1; i < BarCount; i++)
            {
                logIndex[i] = (int)((Math.Log(BarCount, BarCount) - Math.Log(BarCount - i, BarCount)) * (maximumFrequencyIndex - minimumFrequencyIndex) + minimumFrequencyIndex);
                freqVolScalar[i] = 1 + (float)Math.Sqrt((double)i / (double)BarCount);
            }
        }
#endif
        public void Initialize()
        {
            if (Program.DisableAudioProcessing)
            {
                return;
            }
            UpdateThread = new Thread(UpdateLoop);
            UpdateThread.Name = "DataUpdateThread";
            UpdateThread.IsBackground = true;
            UpdateThread.Start();
        }

        public void Cleanup()
        {
            IsClosing = true;
        }

        private void UpdateLoop()
        {
            soundProviderSet.Wait();

            while (!IsClosing)
            {
                volumeBoost = soundProvider.GetVolumeBoost();
                Update();
                FpsLimiter.Sleep();
            }
        }

        private void Update()
        {
            GetBarData(); //Because of KeyboardSpectrum visualizer
            
            if (DataProcessorIndex == 1) GetStereoOscilloscopeData();

            //Update volume
            float CurVol = soundProvider.GetSoundLevel();
            AveragedVolume = UtilityMethods.LinearInterpolate(AveragedVolume, CurVol, 0.005f);

            float[] updatedBarData = barValuesBuffer.GetNext();
            dataPostProcessDelegate?.Invoke(updatedBarData);
            barValuesBuffer.RotateElements();
        }

        private void BuildFrequencyMap()
        {
            #region Hardcoded Frequency Map
            //Borrowed by copypasting the bar values from foobar2000 v1.3.2
            frequencyMap = new int[] {50, 54, 59, 63, 69, 74, 80, 87, 94, 102, 110, 119, 129, 139, 150, 163, 176, 191, 206, 223, 241, 261, 282, 306,
            331, 358, 387, 419, 453, 490, 530, 574, 620, 671, 726, 786, 850, 920, 1000, 1100, 1200, 1300, 1400, 1500, 1600, 1700, 1900, 2000, 2200,
            2400, 2600, 2800, 3000, 3200, 3500, 3800, 4100, 4400, 4800, 5200, 5600, 6100, 6600, 7100, 7700, 8300, 9000, 10000, 11000, 11500, 12000,
            13000, 14000, 14500, 15000, 16000, 17000, 17600, 18000, 19500};
            #endregion
        }

        //Rewrite this.
        private void GetBarData()
        {
            float smoothingFactor = SmoothingFactor;//Cache smoothing factor to avoid lookups

            //i am good at the programming
            //too many magic values
            soundProvider.GetFFTData(ref fftData, (int)maxFFT);
            int barIndex = 0;

            float[] currentBarValues = new float[BarCount];
            float[] finalBarValues = barValuesBuffer.GetNext();

            for (barIndex = 0; barIndex < BarCount; barIndex++)
            {
                currentBarValues[barIndex] = (float)Math.Sqrt(fftData[logIndex[barIndex]] * volumeBoost) * freqVolScalar[barIndex];
            }

            barIndex = 0;

            float preScaled = currentBarValues[barIndex];
            preScaled += currentBarValues[barIndex + 1];
            preScaled /= 2f;
            finalBarValues[barIndex] = UtilityMethods.LinearInterpolate(UtilityMethods.Clamp(preScaled, 0f, 1f), BarValues[barIndex], smoothingFactor); //Smoothen out the spectrum by averaging it over time

            barIndex++;

            preScaled = currentBarValues[barIndex - 1] * 0.75f;
            preScaled += currentBarValues[barIndex];
            preScaled += currentBarValues[barIndex + 1] * 0.75f;
            preScaled /= 2.5f;
            finalBarValues[barIndex] = UtilityMethods.LinearInterpolate(UtilityMethods.Clamp(preScaled, 0f, 1f), BarValues[barIndex], smoothingFactor);

            for (barIndex = 2; barIndex < 50; barIndex++)
            {
                preScaled = currentBarValues[barIndex - 2] * 0.5f;
                preScaled += currentBarValues[barIndex - 1] * 0.75f;
                preScaled += currentBarValues[barIndex];
                preScaled += currentBarValues[barIndex + 1] * 0.75f;
                preScaled += currentBarValues[barIndex + 2] * 0.5f;
                preScaled /= 3.5f;
                finalBarValues[barIndex] = UtilityMethods.LinearInterpolate(UtilityMethods.Clamp(preScaled, 0f, 1f), BarValues[barIndex], smoothingFactor);
            }
            for (barIndex = 50; barIndex < 999; barIndex++)
            {
                preScaled = currentBarValues[barIndex - 1] * 0.75f;
                preScaled += currentBarValues[barIndex];
                preScaled += currentBarValues[barIndex + 1] * 0.75f;
                preScaled /= 2.5f;
                finalBarValues[barIndex] = UtilityMethods.LinearInterpolate(UtilityMethods.Clamp(preScaled, 0f, 1f), BarValues[barIndex], smoothingFactor);
            }
            preScaled = currentBarValues[barIndex - 1];
            preScaled += currentBarValues[barIndex];
            preScaled /= 2f;
            finalBarValues[barIndex] = UtilityMethods.LinearInterpolate(UtilityMethods.Clamp(preScaled, 0f, 1f), BarValues[barIndex], smoothingFactor);

            float Sum = 0f;
            for (int i = 2; i < 28; i++)
            {
                Sum += (float)Math.Sqrt(finalBarValues[i]); //Prettier scaling > Accurate scaling
            }
            SongBeat = (Sum / 25f);
        }

        private void GetStereoOscilloscopeData()
        {
            //too many magic values
            soundProvider.GetPCMData(ref streamData, (int)((OscilloscopeDataLength * 160) + 2));
            int maxLen = streamData.Length - 1;
            for (int c = 0; c < 2; c++)                                                             //im sorry
            {
                for (int x = 0; x < 2400; x++)                                                      //father for
                {
                    if ((x * 4) * 4 + c > maxLen) continue;                                         //i have
                    if (c == 0)
                    {
                        OscilloscopeValues[0, x] = 0.75f - streamData[(x * 4) * 4 + c] * .25f;      //sinned
                    }
                    else
                    {
                        OscilloscopeValues[1, x] = 0.25f - streamData[(x * 4) * 4 + c] * .25f;      //.
                    }
                }
            }
        }
    }
}
