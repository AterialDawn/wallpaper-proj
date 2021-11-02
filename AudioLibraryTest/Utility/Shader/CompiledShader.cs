using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace player.Utility.Shader
{
    class CompiledShader
    {
        public int Program { get { return iProgram; } }
        public string CompErrorString { get { return compErrorString; } }
        public string ShaderName { get; private set; }

        private int iVertexShader;
        private int iFragShader;
        private int iProgram;
        private string compErrorString = null;

        public CompiledShader(ShaderUtility.ShaderSourceContainer container)
        {
            string vertex = container.VertexSource;
            string frag = container.FragmentSource;

            iProgram = GL.CreateProgram();
            int result;

            iVertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(iVertexShader, vertex);
            GL.CompileShader(iVertexShader);
            GL.GetShader(iVertexShader, ShaderParameter.CompileStatus, out result);
            if (result == 0)
            {
                compErrorString = string.Format("Failed to compile vertex shader!\n{0}", GL.GetShaderInfoLog(iVertexShader));
                iProgram = -1;
                throw new ShaderCompilationException(compErrorString);
            }

            iFragShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(iFragShader, frag);
            GL.CompileShader(iFragShader);
            GL.GetShader(iFragShader, ShaderParameter.CompileStatus, out result);
            if (result == 0)
            {
                compErrorString = string.Format("Failed to compile fragment shader!\n{0}", GL.GetShaderInfoLog(iFragShader));
                iProgram = -1;
                throw new ShaderCompilationException(compErrorString);
            }

            GL.AttachShader(iProgram, iVertexShader);
            GL.AttachShader(iProgram, iFragShader);

            GL.LinkProgram(iProgram);

            GL.GetProgram(iProgram, GetProgramParameterName.LinkStatus, out result);
            if (result == 0)
            {
                compErrorString = string.Format("Failed to link shader program!\n{0}", GL.GetProgramInfoLog(iProgram));
                iProgram = -1;
                throw new ShaderCompilationException(compErrorString);
            }

            GL.DetachShader(iProgram, iVertexShader);
            GL.DetachShader(iProgram, iFragShader);

            ShaderName = container.ShaderName;
        }
    }
}
