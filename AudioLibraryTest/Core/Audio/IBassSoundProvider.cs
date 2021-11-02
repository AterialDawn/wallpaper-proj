using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace player.Core.Audio
{
    //Interface that a sound provider (one that plays sounds/provides pcm,fft data and sound volume must implement)
    public interface IBassSoundProvider
    {
        float GetVolumeBoost();

        float GetSoundLevel();
        void GetFFTData(ref float[] FFTData, int Length);
        void GetPCMData(ref float[] PCMData, int Length);
    }
}
