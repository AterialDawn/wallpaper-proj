using player.Utility.Shader;

namespace player.Shaders
{
    class FontShader : Shader
    {
        public override string ShaderName { get { return "FontShader"; } }

        internal FontShader() : base() { }

        public override void Initialize() { }

        protected override void OnActivate() { }
    }
}
