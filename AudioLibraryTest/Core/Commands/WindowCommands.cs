using player.Core.Input;
using player.Core.Service;
using player.Utility;
using Log = player.Core.Logging.Logger;

namespace player.Core.Commands
{
    class WindowCommands : IService
    {
        public string ServiceName { get { return "WindowCommands"; } }

        internal WindowCommands()
        {
        }

        public void Initialize()
        {
            ConsoleManager consoleMan = ServiceManager.GetService<ConsoleManager>();
            consoleMan.RegisterCommandHandler("SetBottomMost", SetBottomMostCommand);
            consoleMan.RegisterCommandHandler("SetTopMost", SetTopMostCommand);
            consoleMan.RegisterCommandHandler("ResetZOrder", ResetZOrderCommand);
        }

        public void Cleanup()
        {

        }

        private void ResetZOrderCommand(object sender, ConsoleLineReadEventArgs args)
        {
            Win32.SetWindowPos(VisGameWindow.ThisForm.GetHandleOfGameWindow(true), Win32.SetWindowPosLocationFlags.HWND_NOTOPMOST, 0, 0, 0, 0, Win32.SetWindowPosFlags.IgnoreResize | Win32.SetWindowPosFlags.IgnoreMove | Win32.SetWindowPosFlags.DoNotActivate);
            Log.Log("Window's Z-Order has been reset");
        }

        private void SetTopMostCommand(object sender, ConsoleLineReadEventArgs args)
        {
            Win32.SetWindowPos(VisGameWindow.ThisForm.GetHandleOfGameWindow(true), Win32.SetWindowPosLocationFlags.HWND_TOPMOST, 0, 0, 0, 0, Win32.SetWindowPosFlags.IgnoreResize | Win32.SetWindowPosFlags.IgnoreMove | Win32.SetWindowPosFlags.DoNotActivate);
            Log.Log("Window is set as topmost");
        }

        private void SetBottomMostCommand(object sender, ConsoleLineReadEventArgs args)
        {
            Win32.SetWindowPos(VisGameWindow.ThisForm.GetHandleOfGameWindow(true), Win32.SetWindowPosLocationFlags.HWND_BOTTOM, 0, 0, 0, 0, Win32.SetWindowPosFlags.IgnoreResize | Win32.SetWindowPosFlags.IgnoreMove | Win32.SetWindowPosFlags.DoNotActivate);
            Log.Log("Window is set as bottommost");
        }
    }
}
