using player.Utility;
using player.Utility.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
