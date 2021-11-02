using player.Core.Audio;
using player.Utility;
using OpenTK.Graphics.OpenGL;
using player.Core.Service;
using player.Shaders;
using player.Core.Render;
using player.Core;

namespace player.Renderers
{
    class OscilloscopeRenderer : VisualizerRendererBase
    {
        private OscilloscopeShader shader;

        public override SoundDataTypes RequiredDataType { get { return SoundDataTypes.StereoOscilloscope; } }

        public override string VisualizerName { get { return "Oscilloscope"; } }

        public override OpenTK.Vector2 Resolution { get; set; }

        private VertexFloatBuffer lineBuffer;
        private VertexFloatBuffer oscBufferLeft;
        private VertexFloatBuffer oscBufferRight;
        private SoundDataProcessor soundDataProcessor;
        private FpsLimitOverrideContext fpsOverride = null;

        public OscilloscopeRenderer()
        {
            shader = new OscilloscopeShader();

            lineBuffer = new VertexFloatBuffer(VertexFormat.XY_COLOR, 14, BufferUsageHint.StaticDraw, PrimitiveType.Lines);
            lineBuffer.AddVertex(0f, .875f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .875f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(0f, .75f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .75f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(0f, .625f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .625f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(0f, .5f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .5f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(0f, .375f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .375f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(0f, .25f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .25f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(0f, .125f, .5f, .5f, .5f, 1f);
            lineBuffer.AddVertex(1f, .125f, .5f, .5f, .5f, 1f);
            lineBuffer.IndexFromLength();
            lineBuffer.Load();

            oscBufferLeft = new VertexFloatBuffer(VertexFormat.XY_COLOR, SoundDataProcessor.OscilloscopeSamples, BufferUsageHint.DynamicDraw, PrimitiveType.LineStrip);
            for (int i = 0; i < SoundDataProcessor.OscilloscopeSamples; i++) oscBufferLeft.AddVertex(0, 0, 0, 0, 0, 0);
            oscBufferLeft.IndexFromLength();
            oscBufferLeft.Load();

            oscBufferRight = new VertexFloatBuffer(VertexFormat.XY_COLOR, SoundDataProcessor.OscilloscopeSamples, BufferUsageHint.DynamicDraw, PrimitiveType.LineStrip);
            for (int i = 0; i < SoundDataProcessor.OscilloscopeSamples; i++) oscBufferRight.AddVertex(0, 0, 0, 0, 0, 0);
            oscBufferRight.IndexFromLength();
            oscBufferRight.Load();

            soundDataProcessor = ServiceManager.GetService<SoundDataProcessor>();
        }

        public override void Activated()
        {
            fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("Oscilloscope", FpsLimitOverride.Maximum);
        }

        public override void Deactivated()
        {
            fpsOverride.Dispose();
            fpsOverride = null;
        }

        public override void Render(double time)
        {
            shader.Activate();

            float RelativeX = 0f;
            lineBuffer.Draw();

            oscBufferLeft.Clear();
            oscBufferRight.Clear();
            
            for (int x = 0; x < SoundDataProcessor.OscilloscopeSamples; x++)
            {
                RelativeX = (float)x / SoundDataProcessor.OscilloscopeSamples;
                oscBufferLeft.AddVertex(RelativeX, soundDataProcessor.OscilloscopeValues[0, x], 1f, 0f, 0f, 1f);
            }
            oscBufferLeft.Reload();
            
            for (int x = 0; x < SoundDataProcessor.OscilloscopeSamples; x++)
            {
                RelativeX = (float)x / SoundDataProcessor.OscilloscopeSamples;
                oscBufferRight.AddVertex(RelativeX, soundDataProcessor.OscilloscopeValues[1, x], 0f, 0f, 1f, 1f);
            }
            oscBufferRight.Reload();

            oscBufferLeft.Draw();
            oscBufferRight.Draw();
        }
    }
}
