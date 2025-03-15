using player.Core.Service;
using System;
using static ImGuiNET.ImGui;
namespace player.Utility.UI
{
    class IMGWindow : IDisposable
    {
        public event Action OnDrawingEvent;

        string windowName;

        private bool disposedValue;
        public IMGWindow(string windowName)
        {
            this.windowName = windowName;

            ServiceManager.GetService<Core.Render.UI.ImGuiManager>().OnRenderingGui += IMGWindow_OnRenderingGui;
        }

        private void IMGWindow_OnRenderingGui(object sender, EventArgs e)
        {
            if (Begin(windowName))
            {
                OnDraw();

                End();
            }
        }

        protected virtual void OnDraw()
        {
            OnDrawingEvent?.Invoke();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ServiceManager.GetService<Core.Render.UI.ImGuiManager>().OnRenderingGui -= IMGWindow_OnRenderingGui;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
