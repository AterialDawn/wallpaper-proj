using player.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linearstar.Windows.RawInput;
using Log = player.Core.Logging.Logger;

namespace player.Core.Input
{
    static class WindowsHotkeyUtil
    {
        private const int WM_HOTKEY = 786;
        private static IntPtr _windowHandle;
        private static List<KeyContainer> RegisteredHokeys = new List<KeyContainer>();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int RegisterHotKey(IntPtr hWnd, int id, KeyModifiers modifiers, Keys vKeyCode);

        [DllImport("user32.dll")]
        private static extern int UnregisterHotKey(IntPtr hWnd, int id);

        static HotkeyForm form;

        public static event EventHandler<RawInputEventArgs> OnMouseRawInput { add { form.Input += value; } remove { form.Input -= value; } }

        public enum KeyModifiers
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            Windows = 8
        }

        static int keyCounter = 0;

        public static void Init()
        {
            if (form != null) return;
            Thread thr = new Thread((_) =>
            {
                form = new HotkeyForm();
                _windowHandle = form.Handle;
                RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.ExInputSink, _windowHandle);
                Application.Run(form);
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
            });
            thr.SetApartmentState(ApartmentState.STA);
            thr.IsBackground = true;
            thr.Start();
            Log.Log("Hotkey Handler Ready");
        }

        public static bool RegisterHotkey(KeyContainer Container)
        {
            //Check if hotkey is already registered, else add
            if (IsHotkeyRegistered(Container)) return false;
            Container.HotkeyId = ++keyCounter;
            //New key, register it.
            var retVal = (int)form.Invoke((Func<int>)(() => RegisterHotKey(_windowHandle, Container.HotkeyId, Container.GetKeyModifiers(), Container.Key)));
            Log.Log($"RegisterHotKey for id {Container.HotkeyId} returned {retVal}");
            if (retVal == 0) return false;
            RegisteredHokeys.Add(Container);
            return true;
        }

        public static bool IsHotkeyRegistered(KeyContainer Container)
        {
            foreach (KeyContainer CurKey in RegisteredHokeys)
            {
                if (CurKey.Equals(Container)) return true;
            }
            return false;
        }

        private static void InvokeHotkeyIfNeeded(int hotkeyId)
        {
            var hotkey = RegisteredHokeys.Where(c => c.HotkeyId == hotkeyId).FirstOrDefault();
            if (hotkey != null)
            {
                hotkey.SafeFireCallback(hotkey.UserObject);
            }
        }

        class HotkeyForm : Form
        {
            public event EventHandler<RawInputEventArgs> Input;
            public HotkeyForm()
            {
            }

            protected override void WndProc(ref Message msg)
            {
                base.WndProc(ref msg);
                if (msg.Msg == WM_HOTKEY)
                {
                    int hotkeyId = msg.WParam.ToInt32();
                    InvokeHotkeyIfNeeded(hotkeyId);
                }
                else if (msg.Msg == 0x00FF)
                {
                    var data = RawInputData.FromHandle(msg.LParam);
                    Input?.Invoke(this, new RawInputEventArgs(data));
                }
                
            }

            protected override void SetVisibleCore(bool value)
            {
                if (!IsHandleCreated) CreateHandle();
                base.SetVisibleCore(false);
            }
        }

        public class KeyContainer : EventArgs
        {
            public Keys Key { get; private set; }
            public bool Shift { get; private set; }
            public bool Ctrl { get; private set; }
            public Action<object> HotkeyCallback { get; private set; }
            public object UserObject { get; private set; }
            public int HotkeyId { get; set; } = -1;

            public KeyContainer(Keys key, bool shift, bool ctrl, Action<object> callback = null, object userObject = null)
            {
                Key = key;
                Shift = shift;
                Ctrl = ctrl;
                HotkeyCallback = callback;
                UserObject = userObject;
            }

            //safe as in swallow all exceptions lmao
            public void SafeFireCallback(object userState)
            {
                if (HotkeyCallback == null) return;
                try
                {
                    HotkeyCallback(userState);
                }
                catch { }
            }

            public WindowsHotkeyUtil.KeyModifiers GetKeyModifiers()
            {
                var mod = new WindowsHotkeyUtil.KeyModifiers();
                mod |= Shift ? WindowsHotkeyUtil.KeyModifiers.Shift : WindowsHotkeyUtil.KeyModifiers.None;
                mod |= Ctrl ? WindowsHotkeyUtil.KeyModifiers.Control : WindowsHotkeyUtil.KeyModifiers.None;
                return mod;
            }

            public bool Equals(KeyContainer OtherContainer)
            {
                return ((OtherContainer.Key == this.Key) && (OtherContainer.Shift == this.Shift) && (OtherContainer.Ctrl == this.Ctrl));
            }
        }
    }
    class RawInputEventArgs : EventArgs
    {
        public RawInputEventArgs(RawInputData data)
        {
            Data = data;
        }

        public RawInputData Data { get; }
    }
}
