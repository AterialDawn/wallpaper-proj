using player.Core.Render.UI;
using player.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace player.Utility.UI
{
    class PlotgraphHelper : IMGRenderable
    {
        public bool AutoScale { get; set; } = true;
        public float ScaleMin { get { return scaleMin; } set { scaleMin = value; } }
        public float ScaleMax { get { return scaleMax; } set { scaleMax = value; } }
        public float Width { get { return size.X; } set { size.X = value; } }
        public float Height { get { return size.Y; } set { size.Y = value; } }

        string graphName;
        float[] values;
        float scaleMin;
        float scaleMax;
        Vector2 size;
        int ftt = 0;
        public PlotgraphHelper(string graphName, int bufferSize, float scaleMin, float scaleMax, int width = 0, int height = 150)
        {
            this.graphName = graphName;
            this.scaleMin = scaleMin;
            this.scaleMax = scaleMax;
            values = new float[bufferSize];
            size = new Vector2(width, height);
        }

        public void Draw()
        {
            ImGui.PlotLines(graphName, values, 0, "", scaleMin, scaleMax, size, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(float newVal)
        {
            values[ftt++] = newVal;
            if (ftt >= values.Length) ftt = 0;
            if (AutoScale)
            {
                if (newVal > scaleMax) scaleMax = newVal;
                if (newVal < scaleMin) scaleMin = newVal;
            }
        }
    }
}
