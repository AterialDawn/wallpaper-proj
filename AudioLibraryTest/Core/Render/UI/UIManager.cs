using OpenTK;
using player.Core.Input;
using player.Core.Render.UI.Controls;
using player.Core.Service;
using System.Collections.Generic;
using Log = player.Core.Logging.Logger;

namespace player.Core.Render.UI
{
    /// <summary>
    /// The UI Manager. It Manages UI stuff.
    /// </summary>
    class UIManager : IService
    {
        public string ServiceName { get { return "UIManager"; } }

        public ControlBase FocusedControl { get; private set; } = null;
        public TextRenderer TextRenderer { get; private set; } = new TextRenderer();
        public Vector2 UISize { get { return new Vector2(uiSize.X, uiSize.Y); } }

        private Vector2 uiSize = new Vector2(1, 1);
        private ConsoleManager consoleManager;
        private InputManager input;

        private List<ControlBase> RegisteredControls = new List<ControlBase>();

        private int controlIndex = 0;

        public void Cleanup() { }

        public void Initialize()
        {
            input = ServiceManager.GetService<InputManager>();

            input.MouseEvent += Input_MouseEvent;
        }

        private void Input_MouseEvent(object sender, OpenTK.Input.MouseEventArgs e)
        {
            foreach (var control in RegisteredControls)
            {
                if (!control.MouseEntered && control.TransformedBounds.Contains(e.Position))
                {
                    control.MouseEntered = true;
                    control.OnMouseEnter();
                }
                else if (control.MouseEntered && !control.TransformedBounds.Contains(e.Position))
                {
                    control.MouseEntered = false;
                    control.OnMouseLeave();
                }
            }
        }

        public void Initialize(int w, int h)
        {
            TextRenderer.Initialize(w, h);
            uiSize = new Vector2(w, h);
            consoleManager = ServiceManager.GetService<ConsoleManager>();
        }

        public void Resize(int w, int h)
        {
            TextRenderer.Resize(w, h);
            uiSize = new Vector2(w, h);

            foreach (var control in RegisteredControls) control.UISizeChanged();
        }

        /// <summary>
        /// Calling this explicitly will surely break stuff.
        /// </summary>
        /// <param name="control"></param>
        public void RegisterControl(ControlBase control)
        {
            control.SetId(controlIndex++); //this will throw if the control was already registered.
            RegisteredControls.Add(control); //IDs are used as the GetHashCode so this is fine.
        }

        public void UnregisterControl(ControlBase control)
        {
            if (!RegisteredControls.Contains(control))
            {
                Log.Log("Tried to unregister nonexistant control?");
                return;
            }
            RegisteredControls.Remove(control);
        }

        public void Render(double time)
        {
            TextRenderer.Begin();
            foreach (var control in RegisteredControls)
            {
                if (!control.Enabled) continue;
                control.Render(time);
            }
            TextRenderer.RenderAllTextNow();
            consoleManager.Render(time); //seems appropriate to have the UI manager render the console i suppose
            TextRenderer.End();
        }

        public bool FocusControl(ControlBase controlToFocus)
        {
            if (!controlToFocus.CanFocus) return false;

            if (FocusedControl != null)
            {
                ControlBase oldFocused = FocusedControl;
                FocusedControl = null;

                oldFocused.OnFocusLost();
            }

            FocusedControl = controlToFocus;
            FocusedControl.OnFocus();
            return true;
        }
    }
}
