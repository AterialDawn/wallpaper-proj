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
        int blackLocation;

        Vector2 resolution = Vector2.Zero;
        float strength = 0;
        bool blur = true;
        bool black = false;

        public override void Initialize()
        {
            SetUniform(GetUniformLocation("image"), 0);
            resolutionLocation = GetUniformLocation("resolution");
            strengthLocation = GetUniformLocation("strength");
            blurLocation = GetUniformLocation("blur");
            blackLocation = GetUniformLocation("black");
        }

        protected override void OnActivate()
        {
            SetUniform(resolutionLocation, resolution);
            SetUniform(strengthLocation, strength);
            SetUniform(blurLocation, blur);
            SetUniform(blackLocation, black);
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

        public void SetBlackState(bool state)
        {
            this.black = state;
            SetUniform(blackLocation, state);
        }
    }
}
