using player.Core.Input;
using player.Core.Service;
using player.Utility;
using System;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using Log = player.Core.Logging.Logger;

namespace player.Core.Audio
{
    class WasapiSoundManager : IService, IBassSoundProvider
    {
        const float BaseVolumeBoost = 6f;

        public string ServiceName { get { return "WasapiSoundManager"; } }

        public static int DefaultDevice = -1;
        public static float VolumeScalar = 1f;

        private WASAPIPROC WasapiProc;
        private bool deviceInitialized = false;
        private int deviceIndex = -1;
        private float customVolumeBoost = 1f;

        internal WasapiSoundManager()
        {
            WasapiProc = new WASAPIPROC(IgnoreDataProc);
        }

        public void Initialize()
        {
            if (Program.DisableAudioProcessing) return;
            BassWasapi.LoadMe();
            if (!Bass.BASS_Init(0, 48000, 0, IntPtr.Zero))
            {
                Log.Log("Unable to initialize the BASS Library!");
                throw new Exception("Cannot initialize nosound device!");
            }
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, 2000);

            GetDefaultDevice();
            if (DefaultDevice != -1)
            {
                SetDeviceIndex(DefaultDevice);
            }

            ServiceManager.GetService<ConsoleManager>().RegisterCommandHandler("wasapi:scalar", volumeScalarCommand);
        }

        private void volumeScalarCommand(object sender, ConsoleLineReadEventArgs args)
        {
            if(args.Arguments.Length == 0)
            {
                Log.Log("Usage : wasapi:scalar [0.0-2.0]");
                return;
            }
            float newScalar;
            if(!float.TryParse(args.Arguments[0], out newScalar))
            {
                Log.Log("Usage : wasapi:scalar [0.0-2.0]");
                return;
            }
            newScalar = UtilityMethods.Clamp(newScalar, 0f, 2f);
            customVolumeBoost = newScalar;
            Log.Log($"Changed scalar to {newScalar}");
        }

        public void Cleanup()
        {
            BassWasapi.BASS_WASAPI_Stop(true);
            BassWasapi.BASS_WASAPI_Free();
        }

        public float GetVolumeBoost()
        {
            return BaseVolumeBoost * customVolumeBoost;
        }

        public float GetSoundLevel()
        {
            if (!deviceInitialized) return 0f;
            return BassWasapi.BASS_WASAPI_GetDeviceLevel(deviceIndex, -1) * VolumeScalar;
        }

        public void GetFFTData(ref float[] FFTData, int Length)
        {
            BassWasapi.BASS_WASAPI_GetData(FFTData, Length);
        }

        public void GetPCMData(ref float[] PCMData, int Length)
        {
            BassWasapi.BASS_WASAPI_GetData(PCMData, Length);
        }

        public bool SetDeviceIndex(int index)
        {
            if (deviceInitialized)
            {
                Log.Log("Uninitializing WASAPI device");
                BassWasapi.BASS_WASAPI_Stop(true);
                BassWasapi.BASS_WASAPI_Free();
                deviceInitialized = false;
            }

            BASS_WASAPI_DEVICEINFO devInfo = BassWasapi.BASS_WASAPI_GetDeviceInfo(index);
            if (devInfo == null)
            {
                Log.Log("Invalid index or no device at index {0}!", index);
                return false;
            }
            if (!BassWasapi.BASS_WASAPI_Init(index, devInfo.mixfreq, 0, BASSWASAPIInit.BASS_WASAPI_AUTOFORMAT | BASSWASAPIInit.BASS_WASAPI_BUFFER, 0.15f, 0f, WasapiProc, IntPtr.Zero))
            {
                BASSError error = Bass.BASS_ErrorGetCode();
                Log.Log("Unable to initialize WASAPI device {0}! Error = {1}", index, error.ToString());
                return false;
            }
            if (!BassWasapi.BASS_WASAPI_Start())
            {
                BASSError error = Bass.BASS_ErrorGetCode();
                Log.Log("Unable to start WASAPI! " + error.ToString());
                return false;
            }
            Log.Log("Wasapi device #{0} ({1}) initialized", index, devInfo.name);
            deviceIndex = index;

            ServiceManager.GetService<SoundDataProcessor>().SampleFrequency = devInfo.mixfreq;
            ServiceManager.GetService<SoundDataProcessor>().BuildLookupTables(); //this probably should be called from a helper method.

            deviceInitialized = true;
            return true;
        }

        //Probably not the best place to place this but it works and is somewhat logical.
        public void PrintDevicesToConsole()
        {
            var devices = BassWasapi.BASS_WASAPI_GetDeviceInfos();
            Log.Log("There are {0} total Wasapi Devices!", devices.Length);
            for (int i = 0; i < devices.Length; i++)
            {
                var device = devices[i];

                if (!device.IsEnabled || !device.SupportsRecording) continue; //Only care about input devices

                if (device.IsLoopback)
                {
                    Log.Log("{0}) {1} (Loopback)", i, device.name);
                }
                else
                {
                    Log.Log("{0}) {1}", i, device.name);
                }
            }
        }

        private void GetDefaultDevice()
        {
            if (DefaultDevice != -1) return;
            var devices = BassWasapi.BASS_WASAPI_GetDeviceInfos();
            string defaultOutputDevice = "";
            foreach (var device in devices)
            {
                if (device.IsDefault && !device.IsInput) defaultOutputDevice = device.id;
            }

            for (int i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                if (device.IsLoopback && device.id == defaultOutputDevice)
                {
                    DefaultDevice = i;
                    return;
                }
                
            }
        }

        private int IgnoreDataProc(IntPtr buffer, int length, IntPtr user)
        {
            return 1;
        }
    }
}
