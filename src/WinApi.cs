using System;
using System.Runtime.InteropServices;
using System.Text;

namespace headless
{
    public static class WinApi
    {
        [DllImport ( "kernel32.dll" ,
            SetLastError = true ,
            CharSet = CharSet.Auto )]
        public static extern uint SearchPath ( string lpPath ,
            string lpFileName ,
            string lpExtension ,
            int nBufferLength ,
            [MarshalAs ( UnmanagedType.LPTStr )]
            StringBuilder lpBuffer ,
            out IntPtr lpFilePart );
        
        [DllImport("user32", EntryPoint = "CreateDesktopW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags,
            int dwDesiredAccess,
            [In] ref SECURITY_ATTRIBUTES lpsa);

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }
        
        public const int GENERIC_ALL = 0x10000000;
        
        [DllImport("user32.dll", EntryPoint="CloseDesktop", CharSet =  CharSet.Unicode, SetLastError = true)]
        public static extern bool CloseDesktop(IntPtr handle);
        
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)]
        public static extern IntPtr CreateJobObject([In] ref SECURITY_ATTRIBUTES lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", EntryPoint = "CreateJobObject", CharSet=CharSet.Unicode)]
        public static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);
        
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
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
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }
        
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);
        
        [DllImport("kernel32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);
        
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool CloseHandle(IntPtr hHandle);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);
        
        public const UInt32 INFINITE = 0xFFFFFFFF;
        public const UInt32 WAIT_ABANDONED = 0x00000080;
        public const UInt32 WAIT_OBJECT_0 = 0x00000000;
        public const UInt32 WAIT_TIMEOUT = 0x00000102;
        
        [DllImport("kernel32.dll", SetLastError=true)]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
        
        [DllImport("user32.dll")]
        public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags,
            bool fInherit, uint dwDesiredAccess);
        
        [DllImport("user32.dll")]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop,
            EnumDesktopWindowsDelegate lpfn, IntPtr lParam);
        public delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, int lParam);
        
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const uint WM_ENDSESSION = 0x16;
        public const uint WM_QUIT       = 0x0012;
    }
}