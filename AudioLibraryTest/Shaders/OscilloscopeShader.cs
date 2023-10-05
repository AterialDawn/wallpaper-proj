using player.Utility.Shader;

namespace player.Shaders
{
    class OscilloscopeShader : Shader
    {
        public override string ShaderName { get { return "OscilloscopeShader"; } }

        internal OscilloscopeShader() : base() { }

        public override void Initialize() { }

        protected override void OnActivate() { }
    }
}
