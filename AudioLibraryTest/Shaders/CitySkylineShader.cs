using OpenTK;
using player.Utility.Shader;

namespace player.Shaders
{
    class CitySkylineShader : Shader
    {
        public override string ShaderName { get { return "CitySkylineShader"; } }

        private int TimeUniformLocation = 0;
        private int BeatUniformLocation = 0;
        private int VolumeUniformLocation = 0;
        private int ResolutionUniformLocation = 0;

        float time = 0;
        float beat = 0;
        float volume = 0;
        Vector2 resolution = new Vector2();

        internal CitySkylineShader() { }

        public override void Initialize()
        {
            TimeUniformLocation = GetUniformLocation("time");
            BeatUniformLocation = GetUniformLocation("beat");
            VolumeUniformLocation = GetUniformLocation("volume");
            ResolutionUniformLocation = GetUniformLocation("resolution");
        }

        protected override void OnActivate()
        {
            SetUniform(TimeUniformLocation, time);
            SetUniform(BeatUniformLocation, beat);
            SetUniform(VolumeUniformLocation, volume);
            SetUniform(ResolutionUniformLocation, resolution);
        }

        public void SetTime(float time)
        {
            this.time = time;
            SetUniform(TimeUniformLocation, time);
        }

        public void SetBeat(float beat)
        {
            this.beat = beat;
            SetUniform(BeatUniformLocation, beat);
        }

        public void SetVolume(float volume)
        {
            this.volume = volume;
            SetUniform(VolumeUniformLocation, volume);
        }

        public void SetResolution(Vector2 newRes)
        {
            resolution = newRes;
            SetUniform(ResolutionUniformLocation, newRes);
        }
    }
}
