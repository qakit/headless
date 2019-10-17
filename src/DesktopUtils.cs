using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using static System.Console;

namespace headless
{
	public static class DesktopUtils
	{
		public class ProcessInTheJob
		{
			public readonly IntPtr jobHandle;
			public readonly WinApi.PROCESS_INFORMATION processInfo;

			public enum Status {
				JOB_ASSIGNED = 1,
				COULD_NOT_ASSIGN_JOB
			}
			public readonly Status status;

			public ProcessInTheJob(Status status, IntPtr jobHandle, WinApi.PROCESS_INFORMATION processInfo)
			{
				this.jobHandle = jobHandle;
				this.status = status;
				this.processInfo = processInfo;
			}
		}

		public static IntPtr CreateDesktop(string name)
		{
			var sa = new WinApi.SECURITY_ATTRIBUTES();
			sa.nLength = Marshal.SizeOf(sa);
			sa.bInheritHandle = 1;
			return WinApi.CreateDesktop(name, IntPtr.Zero, IntPtr.Zero, 0, WinApi.GENERIC_ALL, ref sa);
		}

		public static void CloseDesktop(IntPtr handle)
		{
			WinApi.CloseDesktop(handle);
		}
		
		public static bool DesktopExists(string desktopName)
		{
			var handle = WinApi.OpenDesktop(desktopName, 0, false, 0);
			WinApi.CloseDesktop(handle);
			return handle != IntPtr.Zero;
		}

		public static void CloseDesktopWindows(string desktopName)
		{
			var handle = WinApi.OpenDesktop(desktopName, 0, false, 0);
			if (handle == IntPtr.Zero)
			{
				WriteLine($"INFO: Desktop '{desktopName}' not found.");
				return;
			}
			var windows = new List<IntPtr>();

			WinApi.EnumDesktopWindows(handle, (hwnd, _) =>
			{
				windows.Add(hwnd);
				return true;
			}, IntPtr.Zero);

			WriteLine($"INFO: Top level windows in the headless desktop: {windows.Count}");
			if (windows.Count > 0)
			{
				foreach (var hWindow in windows)
				{
					var windowHandle = new HandleRef(null, hWindow);
					WriteLine($"INFO: Quitting the '{windows.Count}' top level window.'");
					WinApi.PostMessage(windowHandle, WinApi.WM_ENDSESSION, IntPtr.Zero,
						(IntPtr) 1 /* ENDSESSION_CLOSEAPP */);
					WinApi.PostMessage(windowHandle, WinApi.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
				}

				Thread.Sleep(2000);
				windows.Clear();
				WinApi.EnumDesktopWindows(handle, (hwnd, _) =>
				{
					windows.Add(hwnd);
					return true;
				}, IntPtr.Zero);
				WriteLine($"INFO: Top level windows after sending quit message: {windows.Count}");
			}

			WinApi.CloseDesktop(handle);
		}
		
		public static ProcessInTheJob CreateProcessInTheJob(
			string desktopName,
			string appPath,
			string cmdLine,
			IntPtr jobHandle)
		{
			var newJobHandle = IntPtr.Zero;
			if (jobHandle == IntPtr.Zero)
			{
				newJobHandle = jobHandle = WinApi.CreateJobObject(IntPtr.Zero, null);
				// TODO set limits etc
			}

			const uint NORMAL_PRIORITY_CLASS = 0x0020;
			const uint CREATE_SUSPENDED      = 0x00000004;

			var sInfo = new WinApi.STARTUPINFO();
			sInfo.cb = Marshal.SizeOf(sInfo);
			sInfo.lpDesktop = desktopName;

			var pSec = new WinApi.SECURITY_ATTRIBUTES();
			var tSec = new WinApi.SECURITY_ATTRIBUTES();
			pSec.nLength = Marshal.SizeOf(pSec);
			tSec.nLength = Marshal.SizeOf(tSec);

			string commandLine = "";
			if(!string.IsNullOrEmpty(appPath) && !string.IsNullOrEmpty(cmdLine))
			{
				commandLine = $"{appPath} {cmdLine}";
			}
			else if(! string.IsNullOrEmpty(cmdLine)){
				commandLine = cmdLine;
			}

			var retValue = WinApi.CreateProcess(appPath, commandLine,
				ref pSec,ref tSec,false,
				NORMAL_PRIORITY_CLASS | CREATE_SUSPENDED,
				IntPtr.Zero,null, ref sInfo,out var pInfo);

			WriteLine("Process ID (PID): " + pInfo.dwProcessId);
			WriteLine("Process Handle : " + pInfo.hProcess);

			var r = WinApi.AssignProcessToJobObject(jobHandle, pInfo.hProcess);
			if (!r)
			{
				if(newJobHandle != IntPtr.Zero)
					WinApi.CloseHandle(newJobHandle);
				return new ProcessInTheJob(ProcessInTheJob.Status.COULD_NOT_ASSIGN_JOB, IntPtr.Zero, pInfo);
			}

			// Ensure that killing one process kills the others                
			var extendedInfo = new WinApi.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
			{
				BasicLimitInformation = {
					LimitFlags = (short)WinApi.LimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
				}
			};

			int length = Marshal.SizeOf(typeof(WinApi.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
			IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
			Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

			if (!WinApi.SetInformationJobObject(jobHandle,
				WinApi.JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
				extendedInfoPtr, (uint)length))
				throw new Exception(string.Format("Unable to set information.  Error: {0}", Marshal.GetLastWin32Error()));

			Marshal.FreeHGlobal(extendedInfoPtr);
 
			WinApi.ResumeThread(pInfo.hThread);
			return new ProcessInTheJob(ProcessInTheJob.Status.JOB_ASSIGNED, jobHandle, pInfo);
		}

	}
}