using OpenTK.Graphics.OpenGL;
using player.Core;
using player.Core.Audio;
using player.Core.Render;
using player.Shaders;
using player.Utility;

namespace player.Renderers
{
    class CitySkylineRenderer : VisualizerRendererBase
    {
        public override SoundDataTypes RequiredDataType { get { return SoundDataTypes.BarData; } }

        public override string VisualizerName { get { return "City Skyline"; } }

        public override OpenTK.Vector2 Resolution { get; set; }

        private float time = 0;

        VertexFloatBuffer buffer;

        private CitySkylineShader shader;

        FpsLimitOverrideContext fpsOverride = null;

        public CitySkylineRenderer()
        {
            shader = new CitySkylineShader();

            buffer = new VertexFloatBuffer(VertexFormat.XY, 6);
            buffer.AddVertex(0f, 0f);
            buffer.AddVertex(0f, 1f);
            buffer.AddVertex(1f, 1f);
            buffer.AddVertex(0f, 0f);
            buffer.AddVertex(1f, 1f);
            buffer.AddVertex(1f, 0f);
            buffer.UsageHint = BufferUsageHint.StaticDraw;
            buffer.Load();
        }

        public override void Activated()
        {
            fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("CitySkyline", FpsLimitOverride.Maximum);
        }

        public override void Render(double FrameRenderTime)
        {
            time += (float)FrameRenderTime;
            shader.Activate();
            shader.SetTime(time);
            shader.SetBeat(GetBassBeatMeter());
            shader.SetVolume(GetVolume());
            buffer.Draw();
        }

        public override void Deactivated()
        {
            fpsOverride.Dispose();
            fpsOverride = null;
        }

        public override void Deinitialize()
        {

        }

        public override void ResolutionUpdated()
        {
            shader.Activate();
            shader.SetResolution(Resolution);
        }
    }
}
