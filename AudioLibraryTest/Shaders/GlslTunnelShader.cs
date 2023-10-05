using OpenTK;
using player.Utility.Shader;

namespace player.Shaders
{
    class GlslTunnelShader : Shader
    {
        public override string ShaderName { get { return "GlslTunnel"; } }

        private int TimeUniformLocation = 0;
        private int BeatUniformLocation = 0;
        private int ResolutionUniformLocation = 0;

        private float time = 0;
        private float beat = 0;
        private Vector2 resolution = new Vector2();

        internal GlslTunnelShader() : base() { }

        public override void Initialize()
        {
            TimeUniformLocation = GetUniformLocation("time");
            BeatUniformLocation = GetUniformLocation("beat");
            ResolutionUniformLocation = GetUniformLocation("resolution");
        }

        protected override void OnActivate()
        {
            SetUniform(TimeUniformLocation, time);
            SetUniform(BeatUniformLocation, beat);
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

        public void SetResolution(Vector2 newRes)
        {
            resolution = newRes;
            SetUniform(ResolutionUniformLocation, newRes);
        }

    }
}
