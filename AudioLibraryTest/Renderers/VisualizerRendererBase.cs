using player.Core.Audio;
using OpenTK;
using player.Core.Service;

namespace player.Renderers
{
    public abstract class VisualizerRendererBase
    {
        public abstract SoundDataTypes RequiredDataType { get; }

        public abstract string VisualizerName { get; }

        public abstract Vector2 Resolution { get; set; }

        public virtual void Activated() { }

        public virtual void Render(double FrameRenderTime) { }

        public virtual void Deactivated() { }

        public virtual void Deinitialize() { }

        public virtual void ResolutionUpdated() { }

        public void Initialize()
        {
            SoundDataProcessor = ServiceManager.GetService<SoundDataProcessor>();
        }

        protected SoundDataProcessor SoundDataProcessor;

        protected float GetBassBeatMeter()
        {
            if (RequiredDataType != SoundDataTypes.BarData)
            {
                return -1f;
            }
            else
            {
                return SoundDataProcessor.SongBeat;
            }
        }

        protected float GetVolume()
        {
            return SoundDataProcessor.AveragedVolume;
        }
    }
}
