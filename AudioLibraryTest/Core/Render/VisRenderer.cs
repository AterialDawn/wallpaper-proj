using player.Core.Audio;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using player.Renderers;
using player.Utility;
using QuickFont;
using System.Collections.Generic;
using System.Drawing;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render
{
    public class VisRenderer : IService
    {
        List<VisualizerRendererBase> Renderers = new List<VisualizerRendererBase>();
        int RendererIndex = 0;

        public string ServiceName { get { return "VisRenderer"; } }

        private OLabel rendererNameMessage;
        double timeToShowName = 5.0;

        internal VisRenderer()
        {

        }

        public void Initialize()
        {
            Log.Log("Initializing renderers.");
            using (new ProfilingHelper("Renderer Construction"))
            {
                Renderers.Add(new BarRenderer());
                Renderers.Add(new OscilloscopeRenderer());
                Renderers.Add(new GlslTunnelRenderer());
                Renderers.Add(new CitySkylineRenderer());
            }

            using (new ProfilingHelper("Renderer Initialization"))
            {
                foreach (var renderer in Renderers) renderer.Initialize();
            }

            Renderers[0].Activated();
            rendererNameMessage = new OLabel("VisualizerName", Renderers[0].VisualizerName, QFontAlignment.Left, true);
            rendererNameMessage.Location = new PointF(0f, 12f);
            Log.Log("{0} renderers initialized.", Renderers.Count);
        }

        public void Cleanup()
        {
            Renderers.ForEach(renderer => renderer.Deinitialize());
        }

        public void NextRenderer()
        {
            Renderers[RendererIndex].Deactivated();
            RendererIndex = (RendererIndex + 1) % Renderers.Count;
            activateNewRenderer();
        }

        public void PreviousRenderer()
        {
            Renderers[RendererIndex].Deactivated();
            RendererIndex--;
            if (RendererIndex < 0) RendererIndex = Renderers.Count - 1;
            activateNewRenderer();
        }

        private void activateNewRenderer()
        {
            ServiceManager.GetService<SoundDataProcessor>().SetDataProcessor(Renderers[RendererIndex].RequiredDataType);
            Renderers[RendererIndex].Activated();
            rendererNameMessage.Text = Renderers[RendererIndex].VisualizerName;
            rendererNameMessage.Enabled = true;
            timeToShowName = 5.0;
        }

        public void ResolutionChange(int w, int h)
        {
            Renderers.ForEach(renderer => { renderer.Resolution = new OpenTK.Vector2(w, h); renderer.ResolutionUpdated(); });
        }

        public void Render(double TimeToRender)
        {
            if (timeToShowName > 0)
            {
                timeToShowName -= TimeToRender;
                if (timeToShowName < 0)
                {
                    rendererNameMessage.Enabled = false;
                }
            }
            Renderers[RendererIndex].Render(TimeToRender);
        }
    }
}
