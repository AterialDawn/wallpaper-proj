using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Log = player.Core.Logging.Logger;

namespace player.Core.Input
{
    class FocusedInputGrabberForm : Form
    {
        public FocusedInputGrabberForm(Action<KeyEventArgs> onKeyDownHandler, Action<KeyEventArgs> onKeyUpHandler, Action onClosedHandler)
        {
            this.onKeyDownHandler = onKeyDownHandler;
            this.onKeyUpHandler = onKeyUpHandler;
            this.onClosedHandler = onClosedHandler;
        }

        Action<KeyEventArgs> onKeyDownHandler;
        Action<KeyEventArgs> onKeyUpHandler;
        Action onClosedHandler;

        System.Windows.Forms.Timer tmr = null;
        bool lostFocus = false;
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

            Location = new System.Drawing.Point(-9000, -9000);

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

        protected override void OnLostFocus(EventArgs e)
        {
            lostFocus = true;
            this.Close();
        }
    }
}
