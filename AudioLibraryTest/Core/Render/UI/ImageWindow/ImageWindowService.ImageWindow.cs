using OpenTK.Graphics.OpenGL;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using player.Renderers.BarHelpers;
using player.Shaders;
using System.Drawing;
using System.IO;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render.UI.ImageWindow
{
    partial class ImageWindowService
    {
        class ImageWindow : ControlBase
        {
            public IMGWindowData Data { get; private set; }
            IBackground img = null;

            public override bool CanSelect => true;

            public override bool CanFocus => false;

            public override string Name => _name;
            private string _name;

            public SizeF ImageSize { get; private set; }
            public bool Initialized { get; set; } = false;

            Primitives primitives;
            ShaderManager shadMgr;
            TexturedQuadShader shad;

            public ImageWindow(IMGWindowData data)
            {
                Data = data;
                img = ServiceManager.GetService<BackgroundFactory>().LoadFromPath(Data.Path);
                if (img == null)
                {
                    Log.Log($"IMW Could not load {Data.Path}!");
                    throw new FileNotFoundException(Data.Path);
                }

                if (img is AnimatedImageBackground aib)
                {
                    aib.LoadAllFramesToMemory = true;
                }
                _name = $"IMW : {Path.GetFileName(img.SourcePath)}##{data.Path.GetHashCode()}"; //add ## label to avoid collision in case multiple files with the same name are loaded
                img.Preload();

                ImageSize = img.Resolution;

                primitives = ServiceManager.GetService<Primitives>();
                shadMgr = ServiceManager.GetService<ShaderManager>();
                shad = new TexturedQuadShader();

                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
                Size = ImageSize;
            }

            public override void Render(double time)
            {
                LoadMatrix();
                img.Update(time);

                GL.BindTexture(TextureTarget.Texture2D, img.GetTextureIndex());

                shadMgr.PushActiveShader(shad);
                primitives.QuadBuffer.Draw();

                PopMatrix();
                shadMgr.PopActiveShader();
            }

            public override void Dispose()
            {
                base.Dispose();

                img.Destroy();
            }
        }
    }
}
