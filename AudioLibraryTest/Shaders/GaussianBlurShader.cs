using OpenTK;
using player.Utility.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Shaders
{
    class GaussianBlurShader : Shader
    {
        public override string ShaderName => "GaussianBlurShader";

        int resolutionLocation;
        int strengthLocation;
        int blurLocation;
        int colorOverrideLocation;
        int colorLocation;

        Vector2 resolution = Vector2.Zero;
        float strength = 0;
        bool blur = true;
        bool colorOverride = false;
        Vector4 color = new Vector4();

        public override void Initialize()
        {
            SetUniform(GetUniformLocation("image"), 0);
            resolutionLocation = GetUniformLocation("resolution");
            strengthLocation = GetUniformLocation("strength");
            blurLocation = GetUniformLocation("blur");
            colorOverrideLocation = GetUniformLocation("colorOverride");
            colorLocation = GetUniformLocation("color");
        }

        protected override void OnActivate()
        {
            SetUniform(resolutionLocation, resolution);
            SetUniform(strengthLocation, strength);
            SetUniform(blurLocation, blur);
            SetUniform(colorOverrideLocation, colorOverride);
            SetUniform(colorLocation, color);
        }

        public void SetResolution(Vector2 resolution)
        {
            this.resolution = resolution;
            SetUniform(resolutionLocation, resolution);
        }

        public void SetStrength(float strength)
        {
            this.strength = strength;
            SetUniform(strengthLocation, strength);
        }

        public void SetBlurState(bool state)
        {
            this.blur = state;
            SetUniform(blurLocation, state);
        }

        public void SetColorOverride(bool state, Vector4 color)
        {
            this.colorOverride = state;
            this.color = color;
            SetUniform(colorOverrideLocation, state);
            SetUniform(colorLocation, color);
        }
    }
}
