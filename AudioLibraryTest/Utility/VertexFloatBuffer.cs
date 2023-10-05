#define DisableVertexSafety
using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.CompilerServices;

namespace player.Utility
{
    public class VertexFloatBuffer
    {
        public VertexFormat Format { get; private set; }
        public int Stride { get; private set; }
        public int AttributeCount { get; private set; }
        public int TriangleCount { get { return index_data.Length / 3; } }
        public int VertexCount { get { return vertex_data.Length / AttributeCount; } }
        public bool IsLoaded { get; private set; }
        public BufferUsageHint UsageHint { get; set; }
        public PrimitiveType DrawMode { get; set; }

        public int VBO { get { return id_vbo; } }
        public int EBO { get { return id_ebo; } }

        private int id_vbo;
        private int id_ebo;

        private int vertex_position;
        private int index_position;

        protected float[] vertex_data;
        protected uint[] index_data;

        public VertexFloatBuffer(VertexFormat format, int limit = 1024, BufferUsageHint bufferHint = BufferUsageHint.StreamDraw, PrimitiveType beginMode = PrimitiveType.Triangles)
        {
            Format = format;
            SetStride();
            UsageHint = bufferHint;
            DrawMode = beginMode;

            vertex_data = new float[limit * AttributeCount];
            index_data = new uint[limit];
        }

        public void Clear()
        {
            vertex_position = 0;
            index_position = 0;
        }

        public void SetFormat(VertexFormat format)
        {
            Format = format;
            SetStride();
            Clear();
        }

        private void SetStride()
        {
            switch (Format)
            {
                case VertexFormat.XY:
                    Stride = 8;
                    break;
                case VertexFormat.XY_COLOR:
                    Stride = 24;
                    break;
                case VertexFormat.XY_UV:
                    Stride = 16;
                    break;
                case VertexFormat.XY_UV_COLOR:
                    Stride = 32;
                    break;
                case VertexFormat.XYZ:
                    Stride = 12;
                    break;
                case VertexFormat.XYZ_COLOR:
                    Stride = 28;
                    break;
                case VertexFormat.XYZ_UV:
                    Stride = 20;
                    break;
                case VertexFormat.XYZ_UV_COLOR:
                    Stride = 36;
                    break;
                case VertexFormat.XYZ_NORMAL_UV:
                    Stride = 32;
                    break;
                case VertexFormat.XYZ_NORMAL_UV_COLOR:
                    Stride = 48;
                    break;
            }

            AttributeCount = Stride / sizeof(float);
        }

        public void Set(float[] vertices, uint[] indices)
        {
            Clear();
            vertex_data = vertices;
            index_data = indices;
        }

        /// <summary>
        /// Load vertex buffer into a VBO in OpenGL
        /// :: store in memory
        /// </summary>
        public void Load()
        {
            if (IsLoaded) return;

            //VBO
            GL.GenBuffers(1, out id_vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertex_position * sizeof(float)), vertex_data, UsageHint);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.GenBuffers(1, out id_ebo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(index_position * sizeof(uint)), index_data, UsageHint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            IsLoaded = true;
        }

        /// <summary>
        /// Reload the buffer data without destroying the buffers pointer to OpenGL
        /// </summary>
        public void Reload()
        {
            if (!IsLoaded) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertex_position * sizeof(float)), vertex_data, UsageHint);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(index_position * sizeof(uint)), index_data, UsageHint);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        /// <summary>
        /// Unload vertex buffer from OpenGL
        /// :: release memory
        /// </summary>
        public void Unload()
        {
            if (!IsLoaded) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertex_position * sizeof(float)), IntPtr.Zero, UsageHint);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(index_position * sizeof(uint)), IntPtr.Zero, UsageHint);

            GL.DeleteBuffers(1, ref id_vbo);
            GL.DeleteBuffers(1, ref id_ebo);

            IsLoaded = false;
        }

        public void DrawIndexed()
        {
            if (!IsLoaded) return;

            GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);

            switch (Format)
            {
                case VertexFormat.XY:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    break;
                case VertexFormat.XY_COLOR:

                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 8);
                    break;
                case VertexFormat.XY_UV:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 8);
                    break;
                case VertexFormat.XY_UV_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 8);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 16);
                    break;
                case VertexFormat.XYZ:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    break;
                case VertexFormat.XYZ_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 12);
                    break;
                case VertexFormat.XYZ_UV:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 12);
                    break;
                case VertexFormat.XYZ_UV_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 12);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 20);
                    break;
                case VertexFormat.XYZ_NORMAL_UV:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.NormalArray);
                    GL.NormalPointer(NormalPointerType.Float, Stride, 12);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 24);
                    break;
                case VertexFormat.XYZ_NORMAL_UV_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.NormalArray);
                    GL.NormalPointer(NormalPointerType.Float, Stride, 12);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 24);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 32);
                    break;
            }

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, id_ebo);
            GL.DrawElements(DrawMode, index_position, DrawElementsType.UnsignedInt, 0);

            GL.PopClientAttrib();
        }

        public void Draw()
        {
            if (!IsLoaded) return;

            GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
            GL.BindBuffer(BufferTarget.ArrayBuffer, id_vbo);

            switch (Format)
            {
                case VertexFormat.XY:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    break;
                case VertexFormat.XY_COLOR:

                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 8);
                    break;
                case VertexFormat.XY_UV:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 8);
                    break;
                case VertexFormat.XY_UV_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(2, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 8);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 16);
                    break;
                case VertexFormat.XYZ:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    break;
                case VertexFormat.XYZ_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 12);
                    break;
                case VertexFormat.XYZ_UV:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 12);
                    break;
                case VertexFormat.XYZ_UV_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 12);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 20);
                    break;
                case VertexFormat.XYZ_NORMAL_UV:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.NormalArray);
                    GL.NormalPointer(NormalPointerType.Float, Stride, 12);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 24);
                    break;
                case VertexFormat.XYZ_NORMAL_UV_COLOR:
                    GL.EnableClientState(ArrayCap.VertexArray);
                    GL.VertexPointer(3, VertexPointerType.Float, Stride, 0);
                    GL.EnableClientState(ArrayCap.NormalArray);
                    GL.NormalPointer(NormalPointerType.Float, Stride, 12);
                    GL.EnableClientState(ArrayCap.TextureCoordArray);
                    GL.TexCoordPointer(2, TexCoordPointerType.Float, Stride, 24);
                    GL.EnableClientState(ArrayCap.ColorArray);
                    GL.ColorPointer(4, ColorPointerType.Float, Stride, 32);
                    break;
            }
            GL.DrawArrays(DrawMode, 0, vertex_data.Length / AttributeCount);

            GL.PopClientAttrib();
        }

        public void Dispose()
        {
            Unload();
            Clear();
            vertex_data = null;
            index_data = null;
        }

        /// <summary>
        /// Add indices in order of vertices length,
        /// this is if you dont want to set indices and just index from vertex-index
        /// </summary>
        public void IndexFromLength()
        {
            int count = vertex_position / AttributeCount;
            index_position = 0;
            for (uint i = 0; i < count; i++)
            {
                index_data[index_position++] = i;
            }
        }

        public void AddIndex(uint indexA, uint indexB, uint indexC)
        {
            index_data[index_position++] = indexA;
            index_data[index_position++] = indexB;
            index_data[index_position++] = indexC;
        }

        public void AddIndices(uint[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                index_data[index_position++] = indices[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XY)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position] = x;
            vertex_data[vertex_position + 1] = y;
            vertex_position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float u, float v)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XY_UV)
                throw new FormatException("vertex must be of same format type as buffer");
#endif

            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
            vertex_data[vertex_position++] = u;
            vertex_data[vertex_position++] = v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float r, float g, float b, float a)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XY_COLOR)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position] = x;
            vertex_data[vertex_position + 1] = y;
            vertex_data[vertex_position + 2] = r;
            vertex_data[vertex_position + 3] = g;
            vertex_data[vertex_position + 4] = b;
            vertex_data[vertex_position + 5] = a;
            vertex_position += 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float z)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XYZ)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
            vertex_data[vertex_position++] = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float z, float r, float g, float b, float a)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XYZ_COLOR)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
            vertex_data[vertex_position++] = z;
            vertex_data[vertex_position++] = r;
            vertex_data[vertex_position++] = g;
            vertex_data[vertex_position++] = b;
            vertex_data[vertex_position++] = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float z, float u, float v)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XYZ_UV)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
            vertex_data[vertex_position++] = z;
            vertex_data[vertex_position++] = u;
            vertex_data[vertex_position++] = v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float z, float u, float v, float r, float g, float b, float a)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XYZ_UV_COLOR)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
            vertex_data[vertex_position++] = z;
            vertex_data[vertex_position++] = u;
            vertex_data[vertex_position++] = v;
            vertex_data[vertex_position++] = r;
            vertex_data[vertex_position++] = g;
            vertex_data[vertex_position++] = b;
            vertex_data[vertex_position++] = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float z, float nx, float ny, float nz, float u, float v)
        {
#if (!DisableVertexSafety)
            if (!(Format == VertexFormat.XYZ_NORMAL_UV || Format == VertexFormat.XY_UV_COLOR))
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position] = x;
            vertex_data[vertex_position + 1] = y;
            vertex_data[vertex_position + 2] = z;
            vertex_data[vertex_position + 3] = nx;
            vertex_data[vertex_position + 4] = ny;
            vertex_data[vertex_position + 5] = nz;
            vertex_data[vertex_position + 6] = u;
            vertex_data[vertex_position + 7] = v;
            vertex_position += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(float x, float y, float z, float nx, float ny, float nz, float u, float v, float r, float g, float b, float a)
        {
#if (!DisableVertexSafety)
            if (Format != VertexFormat.XYZ_NORMAL_UV_COLOR)
                throw new FormatException("vertex must be of the same format type as buffer");
#endif

            vertex_data[vertex_position++] = x;
            vertex_data[vertex_position++] = y;
            vertex_data[vertex_position++] = z;
            vertex_data[vertex_position++] = nx;
            vertex_data[vertex_position++] = ny;
            vertex_data[vertex_position++] = nz;
            vertex_data[vertex_position++] = u;
            vertex_data[vertex_position++] = v;
            vertex_data[vertex_position++] = r;
            vertex_data[vertex_position++] = g;
            vertex_data[vertex_position++] = b;
            vertex_data[vertex_position++] = a;
        }
    }
}
