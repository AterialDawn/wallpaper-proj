using player.Utility.Shader;

namespace player.Shaders
{
    class TexturedQuadShader : Shader
    {
        public override string ShaderName { get { return "TexturedQuadShader"; } }

        int opacityLoc = 0;
        float _opacity = 1;
        public float Opacity { get { return _opacity; } set { _opacity = value; SetUniform(opacityLoc, _opacity); } }

        internal TexturedQuadShader() : base() { }

        public override void Initialize()
        {
            SetUniform(GetUniformLocation("tex"), 0);
            opacityLoc = GetUniformLocation("opacity");
        }

        protected override void OnActivate()
        {
            Opacity = Opacity;
        }
    }
}
