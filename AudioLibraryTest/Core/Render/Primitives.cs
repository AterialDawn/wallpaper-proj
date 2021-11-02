using OpenTK.Graphics.OpenGL;
using player.Core.Service;
using player.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Core.Render
{
    class Primitives : IService
    {
        public string ServiceName { get { return "Primitives"; } }

        public VertexFloatBuffer QuadBuffer;
        public VertexFloatBuffer CenteredQuad;

        public void Cleanup()
        {
            
        }

        public void Initialize()
        {
            QuadBuffer = new VertexFloatBuffer(VertexFormat.XY_UV_COLOR, 6);
            QuadBuffer.AddVertex(0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);
            QuadBuffer.AddVertex(0f, 1f, 0f, 1f, 1f, 1f, 1f, 1f);
            QuadBuffer.AddVertex(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);
            QuadBuffer.AddVertex(0f, 0f, 0f, 0f, 1f, 1f, 1f, 1f);
            QuadBuffer.AddVertex(1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f);
            QuadBuffer.AddVertex(1f, 0f, 1f, 0f, 1f, 1f, 1f, 1f);
            QuadBuffer.UsageHint = BufferUsageHint.StaticDraw;
            QuadBuffer.Load();

            CenteredQuad = new VertexFloatBuffer(VertexFormat.XY_UV_COLOR, 6);
            CenteredQuad.AddVertex(-.5f, -.5f, 0f, 0f, 1f, 1f, 1f, 1f);
            CenteredQuad.AddVertex(-.5f, .5f, 0f, 1f, 1f, 1f, 1f, 1f);
            CenteredQuad.AddVertex(.5f, .5f, 1f, 1f, 1f, 1f, 1f, 1f);
            CenteredQuad.AddVertex(-.5f, -.5f, 0f, 0f, 1f, 1f, 1f, 1f);
            CenteredQuad.AddVertex(.5f, .5f, 1f, 1f, 1f, 1f, 1f, 1f);
            CenteredQuad.AddVertex(.5f, -.5f, 1f, 0f, 1f, 1f, 1f, 1f);
            CenteredQuad.UsageHint = BufferUsageHint.StaticDraw;
            CenteredQuad.Load();
        }
    }
}
