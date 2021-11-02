using OpenTK;
using player.Utility.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Shaders
{
    class PieChartShader : Shader
    {
        public override string ShaderName => "PieChartShader";
        int fillPercentageLocation;
        int fillColorLocation;

        float fillPercentage = 0;
        Vector4 fillColor = Vector4.Zero;

        public override void Initialize()
        {
            fillPercentageLocation = GetUniformLocation("FillPercentage");
            fillColorLocation = GetUniformLocation("FillColor");
        }

        protected override void OnActivate()
        {
            SetUniform(fillPercentageLocation, fillPercentage);
            SetUniform(fillColorLocation, fillColor);
        }

        public void SetFillPercentage(float percent)
        {
            fillPercentage = percent;
            SetUniform(fillPercentageLocation, fillPercentage);
        }

        public void SetFillColor(Vector4 color)
        {
            fillColor = color;
            SetUniform(fillColorLocation, fillColor);
        }
    }
}
