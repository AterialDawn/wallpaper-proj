//Borrowed from http://stackoverflow.com/a/24339722 by Antosha on 5/16/17 11:55 PM
//Tweaked
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
namespace ProcessCreator
{
    public class ProcessCreator
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcess(
            string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        public static bool RelaunchSelfWithoutConsole(string[] args)
        {
            const uint DETACHED_PROCESS = 0x00000008;

            var pInfo = new PROCESS_INFORMATION();
            var sInfoEx = new STARTUPINFOEX();
            sInfoEx.StartupInfo.cb = Marshal.SizeOf(sInfoEx);
            IntPtr lpValue = IntPtr.Zero;

            try
            {

                var pSec = new SECURITY_ATTRIBUTES();
                var tSec = new SECURITY_ATTRIBUTES();
                pSec.nLength = Marshal.SizeOf(pSec);
                tSec.nLength = Marshal.SizeOf(tSec);
                var lpApplicationName = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                var commandArgs = string.Join(" ", args.Select(arg => (arg.Contains(' ') ? arg : $"\"{arg}\"")));
                var retVal = CreateProcess(lpApplicationName, $"{commandArgs} -relaunched", ref pSec, ref tSec, false, DETACHED_PROCESS, IntPtr.Zero, null, ref sInfoEx, out pInfo);
                return retVal;
            }
            finally
            {
                // Free the attribute list
                if (sInfoEx.lpAttributeList != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(sInfoEx.lpAttributeList);
                }
                Marshal.FreeHGlobal(lpValue);

                // Close process and thread handles
                if (pInfo.hProcess != IntPtr.Zero)
                {
                    CloseHandle(pInfo.hProcess);
                }
                if (pInfo.hThread != IntPtr.Zero)
                {
                    CloseHandle(pInfo.hThread);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
    }
}