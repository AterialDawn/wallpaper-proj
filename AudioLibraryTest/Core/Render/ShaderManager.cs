using OpenTK.Graphics.OpenGL;
using player.Core.Service;
using player.Utility.Shader;
using System.Collections.Generic;

namespace player.Core.Render
{
    //this class is not threadsafe.
    class ShaderManager : IService
    {
        private Dictionary<string, CompiledShader> shaderCache = new Dictionary<string, CompiledShader>();

        private Shader activeShader = null;

        public string ServiceName { get { return "ShaderManager"; } }

        private Stack<Shader> shaderStack = new Stack<Shader>();

        internal ShaderManager()
        {

        }

        public void Cleanup() { }

        public void Initialize() { }

        public void SetActiveShader(Shader shader)
        {
            if (activeShader != shader)
            {
                activeShader = shader;
                if (activeShader != null)
                {
                    GL.UseProgram(shader.Program);
                }
                else
                {
                    GL.UseProgram(0);
                }
            }
        }

        public bool IsActiveShader(Shader shader) => activeShader == shader;

        public void PushActiveShader(Shader shader)
        {
            shaderStack.Push(activeShader);
            shader.Activate();
            activeShader = shader;
        }

        public void PopActiveShader()
        {
            if (shaderStack.Count > 0)
            {
                SetActiveShader(shaderStack.Pop());
            }
        }

        public CompiledShader GetShader(Shader source)
        {
            if (!shaderCache.ContainsKey(source.ShaderName))
            {
                CompiledShader compiledShader = new CompiledShader(ShaderUtility.LoadShaderFromResource(source.ShaderName));
                shaderCache.Add(source.ShaderName, compiledShader);
                Logging.Logger.Log($"Caching a new shader! {source.ShaderName}");
            }

            return shaderCache[source.ShaderName];
        }
    }
}
