using player.Utility;
using player.Utility.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Shaders
{
    class TexturedQuadShader : Shader
    {
        public override string ShaderName { get { return "TexturedQuadShader"; } }

        internal TexturedQuadShader() : base() { }

        public override void Initialize() { }

        protected override void OnActivate() { }
    }
}
