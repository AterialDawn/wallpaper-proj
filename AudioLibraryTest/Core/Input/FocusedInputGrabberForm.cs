using OpenTK.Input;
using player.Core.Render.UI;
using player.Core.Service;
using player.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Log = player.Core.Logging.Logger;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace player.Core.Input
{
    class FocusedInputGrabberForm : Form
    {
        static MethodInfo setButtonInfo;
        static FocusedInputGrabberForm()
        {
            setButtonInfo = typeof(OpenTK.Input.MouseEventArgs).GetMethod("SetButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (setButtonInfo == null)
            {
                Log.Log($"SetButtonInfo was null...");
            }
        }
        InputManager input;
        ImGuiManager imGui;
        public FocusedInputGrabberForm(Action<KeyEventArgs> onKeyDownHandler, Action<KeyEventArgs> onKeyUpHandler, Action onClosedHandler)
        {
            this.onKeyDownHandler = onKeyDownHandler;
            this.onKeyUpHandler = onKeyUpHandler;
            this.onClosedHandler = onClosedHandler;

            input = ServiceManager.GetService<InputManager>();
            imGui = ServiceManager.GetService<ImGuiManager>();

            if (VisGameWindow.FormWallpaperMode != WallpaperMode.None)
            {
                currentBounds = WallpaperUtils.GetDesktopBounds(false);
            }
            else
            {
                currentBounds = VisGameWindow.ThisForm.Bounds;
            }

            FormBorderStyle = FormBorderStyle.None;
            Location = currentBounds.Location;
            Size = currentBounds.Size;
            this.Opacity = 0.01;
        }

        Action<KeyEventArgs> onKeyDownHandler;
        Action<KeyEventArgs> onKeyUpHandler;
        Action onClosedHandler;
        int lastX = -1;
        int lastY = -1;

        Timer tmr = null;
        bool lostFocus = false;
        Rectangle currentBounds;
        protected override void OnShown(EventArgs e)
        {
            tmr = new Timer();
            tmr.Tick += (s, args) =>
            {
                if (lostFocus) return;
                if (Form.ActiveForm != this)
                {
                    Activate();
                }
                else
                {
                    tmr?.Stop();
                    tmr?.Dispose();
                    tmr = null;
                }
            };
            tmr.Interval = 30;
            tmr.Start();

            //Location = new System.Drawing.Point(-9000, -9000);
            FormBorderStyle = FormBorderStyle.None;
            Location = currentBounds.Location;
            Size = currentBounds.Size;
            this.Opacity = 0.01;

            base.OnShown(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            tmr?.Stop();
            tmr?.Dispose();
            onClosedHandler();
            base.OnClosed(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            onKeyDownHandler(e);
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            onKeyUpHandler(e);
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.Escape) Close();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            imGui.CharPress(e.KeyChar);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            lostFocus = true;
            this.Close();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var mouseArgs = new MouseButtonEventArgs(
                e.X,
                e.Y, 
                e.Button == MouseButtons.Left ? MouseButton.Left : e.Button == MouseButtons.Middle ? MouseButton.Middle : e.Button == MouseButtons.Right ? MouseButton.Right : MouseButton.Button1, 
                true);

            setButtonInfo.Invoke(mouseArgs, new object[] { mouseArgs.Button, OpenTK.Input.ButtonState.Pressed });

            input.MouseDown(mouseArgs);
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            var mouseArgs = new MouseButtonEventArgs(
                e.X,
                e.Y,
                e.Button == MouseButtons.Left ? MouseButton.Left : e.Button == MouseButtons.Middle ? MouseButton.Middle : e.Button == MouseButtons.Right ? MouseButton.Right : MouseButton.Button1,
                false);

            input.MouseDown(mouseArgs);
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var mouseArgs = new MouseWheelEventArgs(e.X, e.Y, e.Delta, e.Delta);
            input.MouseWheel(mouseArgs);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (lastX == -1)
            {
                lastX = e.X;
                lastY = e.Y;
            }
            var mouseArgs = new MouseMoveEventArgs(e.X, e.Y, e.X - lastX, e.Y - lastY);
            lastX = e.X;
            lastY = e.Y;
            input.MouseMove(mouseArgs);
            base.OnMouseMove(e);
        }
    }
}
