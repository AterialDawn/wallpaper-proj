using OpenTK;
using player.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace player.Core.Render.UI.Controls
{
    class PieChartControl : ControlBase
    {
        public override bool CanFocus { get { return true; } }

        public override bool CanSelect { get { return true; } }

        public override string Name { get; }

        /// <summary>
        /// Automatically set size depending on text content
        /// </summary>
        public bool AutoSize { get; set; } = true;

        private float targetPercent = 0;

        PieChartShader shader;
        ShaderManager shaderMgr;
        Primitives primitives;

        /// <summary>
        /// OpenGL Label
        /// </summary>
        /// <param name="name">Name of the label</param>
        /// <param name="text">Text to be displayed on the label</param>
        /// <param name="textChangesFrequently">Optimization hint</param>
        public PieChartControl(string name) : base()
        {
            Name = name;
            shaderMgr = Service.ServiceManager.GetService<ShaderManager>();
            primitives = Service.ServiceManager.GetService<Primitives>();
            shader = new PieChartShader();

            shader.SetFillColor(new Vector4(.5f, .5f, .5f, 0.6f));
            shader.SetFillPercentage(0);
        }

        public override void OnMouseEnter()
        {
            shader.SetFillColor(new Vector4(1f, 1f, 1f, 0.9f));
        }
        public override void OnMouseLeave()
        {
            shader.SetFillColor(new Vector4(.5f, .5f, .5f, 0.6f));
        }

        public void SetFillPercentage(float percent)
        {
            targetPercent = percent;
        }

        public override void Render(double time)
        {
            shaderMgr.PushActiveShader(shader);

            shader.SetFillPercentage(targetPercent);

            LoadMatrix();

            primitives.QuadBuffer.Draw();

            PopMatrix();
            shaderMgr.PopActiveShader();
        }
    }
}
