using OpenTK.Graphics.OpenGL;
using player.Core.Service;
using player.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        public static VertexFloatBuffer GenerateXY_UVRect(RectangleF xy, RectangleF uv)
        {
            var ret = new VertexFloatBuffer(VertexFormat.XY_UV, bufferHint: BufferUsageHint.StreamDraw);

            ret.AddVertex(xy.Left, xy.Bottom, uv.Left, uv.Bottom);
            ret.AddVertex(xy.Left, xy.Top, uv.Left, uv.Top);
            ret.AddVertex(xy.Right, xy.Top, uv.Right, uv.Top);

            ret.AddVertex(xy.Left, xy.Bottom, uv.Left, uv.Bottom);
            ret.AddVertex(xy.Right, xy.Top, uv.Right, uv.Top);
            ret.AddVertex(xy.Right, xy.Bottom, uv.Right, uv.Bottom);
            ret.Load();

            return ret;
        }
    }
}
