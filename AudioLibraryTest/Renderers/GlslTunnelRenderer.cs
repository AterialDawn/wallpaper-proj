using OpenTK.Graphics.OpenGL;
using player.Core;
using player.Core.Audio;
using player.Core.Render;
using player.Shaders;
using player.Utility;
using System;

namespace player.Renderers
{
    class GlslTunnelRenderer : VisualizerRendererBase
    {
        public override SoundDataTypes RequiredDataType { get { return SoundDataTypes.BarData; } }

        public override string VisualizerName { get { return "GLSL Tunnel"; } }

        public override OpenTK.Vector2 Resolution { get; set; }

        private float time = 0;

        private GlslTunnelShader shader;

        private VertexFloatBuffer buffer;

        private FpsLimitOverrideContext fpsOverride = null;

        public GlslTunnelRenderer()
        {
            shader = new GlslTunnelShader();

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
            fpsOverride = VisGameWindow.ThisForm.FpsLimiter.OverrideFps("GLSL Tunnel", FpsLimitOverride.Maximum);
        }

        public override void Render(double FrameRenderTime)
        {
            time += (float)FrameRenderTime;
            float beat = GetBassBeatMeter();
            float scaledBeat = (float)Math.Pow((beat), 3.2) * 0.06f;
            time += scaledBeat;
            shader.Activate();
            shader.SetTime(time);
            shader.SetBeat(beat);
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
