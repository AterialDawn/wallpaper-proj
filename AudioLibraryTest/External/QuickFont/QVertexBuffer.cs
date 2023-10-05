using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;


namespace QuickFont
{
    public class QVertexBuffer : IDisposable
    {
        public int VertexCount = 0;

        int VboID;
        public QVertex[] Vertices = new QVertex[4096]; //should be more than enough for general purposes
        int TextureID;


        public QVertexBuffer(int textureID)
        {
            TextureID = textureID;

            GL.GenBuffers(1, out VboID);
        }

        public void Dispose()
        {
            GL.DeleteBuffers(1, ref VboID);
        }

        public void Reset()
        {
            VertexCount = 0;
        }

        public void AddVertex(Vector2 point, Vector2 textureCoord, int color)
        {
            if (VertexCount + 1 >= Vertices.Length)
            {
                var newArray = new QVertex[Vertices.Length * 2];
                Array.Copy(Vertices, newArray, VertexCount);
                Vertices = newArray;
            }

            Vertices[VertexCount].Set(point, textureCoord, color);

            VertexCount++;
        }

        public void Load()
        {
            if (VertexCount == 0)
                return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, VboID);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(VertexCount * BlittableValueType.StrideOf(Vertices)), Vertices, BufferUsageHint.StaticDraw);
        }

        public void Draw()
        {
            if (VertexCount == 0)
                return;

            GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.EnableClientState(ArrayCap.ColorArray);

            GL.BindTexture(TextureTarget.Texture2D, TextureID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VboID);

            GL.VertexPointer(2, VertexPointerType.Float, BlittableValueType.StrideOf(Vertices), new IntPtr(0));
            GL.TexCoordPointer(2, TexCoordPointerType.Float, BlittableValueType.StrideOf(Vertices), new IntPtr(8));
            GL.ColorPointer(4, ColorPointerType.UnsignedByte, BlittableValueType.StrideOf(Vertices), new IntPtr(16));

            // triangles because quads are depreciated in new opengl versions
            GL.DrawArrays(PrimitiveType.Triangles, 0, VertexCount);

            GL.PopClientAttrib();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct QVertex
    {
        public Vector2 Position;
        public Vector2 TextureCoord;
        public int VertexColor;

        internal void Set(Vector2 point, Vector2 textureCoord, int p)
        {
            Position = point;
            TextureCoord = textureCoord;
            VertexColor = p;
        }
    }
}