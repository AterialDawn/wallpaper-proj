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

        private Dictionary<int, Shader> activeShaderDict = new Dictionary<int, Shader>();

        public string ServiceName { get { return "ShaderManager"; } }

        private Dictionary<int, Stack<Shader>> shaderStackDict = new Dictionary<int, Stack<Shader>>();

        internal ShaderManager()
        {

        }

        public void Cleanup() { }

        public void Initialize() { }

        public void SetActiveShader(Shader shader)
        {
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var activeShader = GetActiveShader();
            if (activeShader != shader)
            {
                activeShader = shader;
                activeShaderDict[threadId] = activeShader;
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

        public Shader GetActiveShader()
        {
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!activeShaderDict.TryGetValue(threadId, out var activeShader))
            {
                activeShaderDict[threadId] = null;
                activeShader = null;
            }

            return activeShader;
        }

        public void PushActiveShader(Shader shader)
        {
            var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!shaderStackDict.TryGetValue(threadId, out var shaderStack))
            {
                shaderStack = new Stack<Shader>();
                shaderStackDict[threadId] = shaderStack;
            }
            var activeShader = activeShaderDict[threadId];
            shaderStack.Push(activeShader);
            shader.Activate();
            activeShader = shader;
            activeShaderDict[threadId] = activeShader;
        }

        public void PopActiveShader()
        {
            var shaderStack = shaderStackDict[System.Threading.Thread.CurrentThread.ManagedThreadId];
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
