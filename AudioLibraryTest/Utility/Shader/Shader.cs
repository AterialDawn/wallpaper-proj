using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using player.Core.Render;
using player.Core.Service;

namespace player.Utility.Shader
{
    abstract class Shader
    {
        private CompiledShader compiledShader;
        protected ShaderManager shaderManager;

        public abstract string ShaderName { get; }

        public int Program { get { return compiledShader.Program; } }
        public string CompliationErrorString { get { return compiledShader.CompErrorString; } }

        protected Shader()
        {
            shaderManager = ServiceManager.GetService<ShaderManager>();
            compiledShader = shaderManager.GetShader(this);
            GL.UseProgram(Program);
            Initialize(); //eh.
        }

        public void SetCompiledShader(CompiledShader compShader)
        {
            compiledShader = compShader;
        }

        #region SetUniform
        protected void SetUniform(int location, Matrix4 value)
        {
            GL.UniformMatrix4(location, false, ref value);
        }

        protected void SetUniform(int location, Vector4 value)
        {
            GL.Uniform4(location, value);
        }

        protected void SetUniform(int location, int value)
        {
            GL.Uniform1(location, value);
        }

        protected void SetUniform(int location, float value)
        {
            GL.Uniform1(location, value);
        }

        protected void SetUniform(int location, double value)
        {
            GL.Uniform1(location, value);
        }
        protected void SetUniform(int location, Vector3 value)
        {
            GL.Uniform3(location, ref value);
        }

        protected void SetUniform(int location, Vector2 value)
        {
            GL.Uniform2(location, ref value);
        }

        protected void SetUniform(int location, bool value)
        {
            GL.Uniform1(location, value ? 1 : 0);
        }
        #endregion

        protected int GetUniformLocation(string Name)
        {
            return GL.GetUniformLocation(Program, Name);
        }

        protected int GetUniformBlockIndex(string Name)
        {
            return GL.GetUniformBlockIndex(Program, Name);
        }

        protected void UniformBlockBinding(int BlockIndex, int BlockBindingIndex)
        {
            GL.UniformBlockBinding(Program, BlockIndex, BlockBindingIndex);
        }

        public void Activate()
        {
            shaderManager.SetActiveShader(this);
            OnActivate();
        }

        public void Deactivate()
        {
            shaderManager.SetActiveShader(null);
        }

        public abstract void Initialize();
        protected abstract void OnActivate();

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string ToString()
        {
            return "Shader Program ID = " + Program;
        }

        public override int GetHashCode()
        {
            return Program.GetHashCode();
        }
    }

    public class ShaderCompilationException : Exception
    {
        public ShaderCompilationException()
            : base()
        {

        }

        public ShaderCompilationException(string Message)
            : base(Message)
        {

        }
    }
}
