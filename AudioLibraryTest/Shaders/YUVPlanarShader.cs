using player.Utility.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Shaders
{
    class YUVPlanarShader : Shader
    {
        public override string ShaderName => "YUVPlanarShader";
        public bool LimitedToFullColorRangeConvert { get { return _limitedConvert; } set { _limitedConvert = value; SetUniform(_limitedConvertLocation, _limitedConvert); } }
        private bool _limitedConvert = false;
        private int _limitedConvertLocation = 0;

        public override void Initialize()
        {
            SetUniform(GetUniformLocation("yTex"), 0);
            SetUniform(GetUniformLocation("uTex"), 1);
            SetUniform(GetUniformLocation("vTex"), 2);
            _limitedConvertLocation = GetUniformLocation("limitedToFullRangeConvert");

        }

        protected override void OnActivate()
        {
            SetUniform(_limitedConvertLocation, _limitedConvert);
        }
    }
}
