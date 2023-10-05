using OpenTK;
using player.Utility.Shader;

namespace player.Shaders
{
    class BarShader : Shader
    {
        public override string ShaderName { get { return "BarShader"; } }
        private int saturationUniform = 0;
        private int texturingUniform = 0;
        private int[] texMatrixUniform = new int[2];
        private int[] texResolutionsUniform = new int[2];
        private int blendValUniform = 0;
        private int blendUniform = 0;
        private int primaryInterpolationUniform = 0;
        private int secondaryInterpolationUniform = 0;

        public InterpolationType PrimaryInterpolation { get; private set; } = BarShader.InterpolationType.None;
        public InterpolationType SecondaryInterpolation { get; private set; } = InterpolationType.None;

        private bool texturingState = false;
        private float saturationAmount = 0;
        private Matrix4 primaryMatrix = new Matrix4();
        private Matrix4 secondaryMatrix = new Matrix4();
        private Vector2 primaryRes = new Vector2();
        private Vector2 secondaryRes = new Vector2();
        private bool blendingState = false;
        private float blendingAmount = 0;

        internal BarShader() : base() { }

        public override void Initialize()
        {
            saturationUniform = GetUniformLocation("saturation");
            texturingUniform = GetUniformLocation("texturing");
            texMatrixUniform[0] = GetUniformLocation("texMatrix[0]");
            texMatrixUniform[1] = GetUniformLocation("texMatrix[1]");
            texResolutionsUniform[0] = GetUniformLocation("texResolutions[0]");
            texResolutionsUniform[1] = GetUniformLocation("texResolutions[1]");
            saturationUniform = GetUniformLocation("saturation");
            blendValUniform = GetUniformLocation("blendValue");
            blendUniform = GetUniformLocation("blending");
            primaryInterpolationUniform = GetUniformLocation("primaryInterpolationType");
            secondaryInterpolationUniform = GetUniformLocation("secondaryInterpolationType");

            SetUniform(GetUniformLocation("tex"), 0); //Bind tex to texture unit 0
            SetUniform(GetUniformLocation("tex2"), 1); //Bind tex2 to texture unit 1
            SetUniform(primaryInterpolationUniform, (int)InterpolationType.None); //default to no interpolation
            SetUniform(secondaryInterpolationUniform, (int)InterpolationType.None);
        }

        protected override void OnActivate()
        {
            SetUniform(texturingUniform, texturingState);
            SetUniform(saturationUniform, saturationAmount);
            SetUniform(texMatrixUniform[0], primaryMatrix);
            SetUniform(texMatrixUniform[1], secondaryMatrix);
            SetUniform(blendUniform, blendingState);
            SetUniform(blendValUniform, blendingAmount);
            SetUniform(primaryInterpolationUniform, (int)PrimaryInterpolation);
            SetUniform(secondaryInterpolationUniform, (int)SecondaryInterpolation);

            SetUniform(texResolutionsUniform[0], primaryRes);
            SetUniform(texResolutionsUniform[1], secondaryRes);
        }

        public void SetPrimaryInterpolationType(InterpolationType type)
        {
            PrimaryInterpolation = type;
            SetUniform(primaryInterpolationUniform, (int)PrimaryInterpolation);
        }

        public void SetSecondaryInterpolationType(InterpolationType type)
        {
            SecondaryInterpolation = type;
            SetUniform(secondaryInterpolationUniform, (int)SecondaryInterpolation);
        }

        public void SetTexturing(bool state)
        {
            texturingState = state;
            SetUniform(texturingUniform, state);
        }

        public void SetSaturation(float value)
        {
            saturationAmount = value;
            SetUniform(saturationUniform, value);
        }

        public void SetPrimaryMatrix(Matrix4 newMatrix)
        {
            primaryMatrix = newMatrix;
            SetUniform(texMatrixUniform[0], newMatrix);
        }

        public void SetSecondaryMatrix(Matrix4 newMatrix)
        {
            secondaryMatrix = newMatrix;
            SetUniform(texMatrixUniform[1], newMatrix);
        }

        public void SetPrimarySize(Vector2 size)
        {
            primaryRes = size;
            SetUniform(texResolutionsUniform[0], primaryRes);
        }

        public void SetSecondarySize(Vector2 size)
        {
            secondaryRes = size;
            SetUniform(texResolutionsUniform[1], secondaryRes);
        }

        public void SetBlendingState(bool state)
        {
            blendingState = state;
            SetUniform(blendUniform, state);
        }

        public void SetBlendingAmount(float amount)
        {
            blendingAmount = amount;
            SetUniform(blendValUniform, amount);
        }

        public enum InterpolationType
        {
            None = 0,
            CatmullRom = 1,
            BiCubic = 2,
            BiLinear = 3,
            BSpline = 4
        }
    }
}
