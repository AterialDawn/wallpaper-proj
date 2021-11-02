using System;
using Log = player.Core.Logging.Logger;

namespace player.Utility.Shader
{
    public static class ShaderUtility
    {
        public static ShaderSourceContainer LoadShaderFromResource(string ExtensionlessShaderName)
        {
            string vertSource;
            string fragSource;
            Log.Log("Attempting to load shader name {0}", ExtensionlessShaderName);
            string vertName = string.Format("player.Shaders.{0}.vert", ExtensionlessShaderName);
            string fragName = string.Format("player.Shaders.{0}.frag", ExtensionlessShaderName);
            var ExecAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            if (ExecAssembly.GetManifestResourceInfo(vertName) == null || ExecAssembly.GetManifestResourceInfo(fragName) == null)
            {
                throw new InvalidOperationException(string.Format("Shader {0} does not exist in the assembly.", ExtensionlessShaderName));
            }
            using (var vertStream = ExecAssembly.GetManifestResourceStream(vertName))
            using (var vertStrReader = new System.IO.StreamReader(vertStream))
            {
                vertSource = vertStrReader.ReadToEnd();
            }

            using (var fragStream = ExecAssembly.GetManifestResourceStream(fragName))
            using (var fragStrReader = new System.IO.StreamReader(fragStream))
            {
                fragSource = fragStrReader.ReadToEnd();
            }
            Log.Log($"Loaded shader {ExtensionlessShaderName} successfully!");
            return new ShaderSourceContainer(vertSource, fragSource, ExtensionlessShaderName);
        }

        public class ShaderSourceContainer
        {

            public string VertexSource { get; private set; }
            public string FragmentSource { get; private set; }
            public string ShaderName { get; private set; }

            public ShaderSourceContainer(string VertSource, string FragSource, string ShaderInternalName)
            {
                VertexSource = VertSource;
                FragmentSource = FragSource;
                ShaderName = ShaderInternalName;
            }
        }
    }
}
