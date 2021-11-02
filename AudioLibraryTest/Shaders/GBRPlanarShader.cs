using player.Utility.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Shaders
{
    class GBRPlanarShader : Shader
    {
        public override string ShaderName => "GBRPlanarShader";

        public override void Initialize()
        {
            SetUniform(GetUniformLocation("greenTex"), 0);
            SetUniform(GetUniformLocation("blueTex"), 1);
            SetUniform(GetUniformLocation("redTex"), 2);
        }

        protected override void OnActivate()
        {
        }
    }
}
