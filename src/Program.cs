using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;

namespace headless
{
	static class Program
	{
		static string FindDriver(string driver)
		{
			var sb = new StringBuilder(260);
			var result = WinApi.SearchPath(null , driver, null , sb.Capacity, sb, out var _);

			return result > 0 ? sb.ToString() : null;
		}

		static readonly object CleanupLock = new object();

		private static void Cleanup(ICollection<Action> cleanupActions)
		{
			lock (CleanupLock)
			{
				WriteLine("INFO: running cleanup");
				foreach (var action in cleanupActions.AsEnumerable().Reverse())
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						WriteLine($"ERR: failed to perform cleanup action. Err: {e.Message}");
					}
				}
				cleanupActions.Clear();
			}
		}

		static int Main(string[] cmdArgs)
		{
			var argMap = Args.Parse(cmdArgs, new HashSet<string> { "driver", "desktop" });
			var cleanupActions = new List<Action>();
			
			WriteLine($"Headless driver started with args '{string.Join(" ", cmdArgs)}'");

			var seleniumDriver =
				(argMap.Named.TryGetValue("driver", out var val) ? val
					: System.Environment.GetEnvironmentVariable("HEADLESS_DRIVER")) ?? "chromedriver.exe";
			var driverPath = FindDriver(seleniumDriver);
			if (driverPath == null)
			{
				WriteLine($"ERR: driver '{seleniumDriver}' not found'");
				return 1;
			}
			WriteLine($"INFO: using driver {driverPath}");

			var rand = new System.Random();

			// shaping desktop name
			string desktopName = null;
			bool useHiddenDesktop = false;

			if (argMap.Named.TryGetValue("desktop", out val) && !string.IsNullOrWhiteSpace(val)) {
				if(val != "false") {
					desktopName = val;
					useHiddenDesktop = true;
				}
			} else {
				desktopName = $"Headless-{rand.Next(int.MaxValue)}";
				useHiddenDesktop = true;
			}

			CancelKeyPress += (s, ev) =>
			{
				WriteLine("Ctrl+C pressed");
				Cleanup(cleanupActions);
				ev.Cancel = true;
			};

			// get desktop name from params
			var desktopHandle = IntPtr.Zero;

			if(useHiddenDesktop) {
				desktopHandle = DesktopUtils.CreateDesktop(desktopName);
				WriteLine($"INFO: opened desktop '{desktopName}'");
				
				cleanupActions.Add(() =>
				{
					WriteLine($"INFO: closing desktop {desktopHandle}");
					DesktopUtils.CloseDesktop(desktopHandle);
				});
			}

			var argsEscaped = from r in argMap.Reminder select $"\"{r}\"";
			var commandLine = String.Join(" ", argsEscaped);
			WriteLine($"INFO: Starting driver with cmdline '{commandLine}'");

			// TODO desktop default
			var jobProcess = DesktopUtils.CreateProcessInTheJob(desktopName, driverPath, commandLine, IntPtr.Zero);
			if (jobProcess.status == DesktopUtils.ProcessInTheJob.Status.COULD_NOT_ASSIGN_JOB)
			{
				WriteLine("ERROR: Failed to assign job");
				WinApi.ResumeThread(jobProcess.processInfo.hThread);
			}
			else
			{
				cleanupActions.Add(() =>
				{
					WriteLine($"INFO: closing job {jobProcess.jobHandle}");
					WinApi.CloseHandle(jobProcess.jobHandle);
				});
			}

			cleanupActions.Add(() =>
			{
				WriteLine($"INFO: closing process {jobProcess.processInfo.hProcess}");
				WinApi.CloseHandle(jobProcess.processInfo.hProcess);
			});

			if(useHiddenDesktop) {
				cleanupActions.Add(() =>
				{
					WriteLine("INFO: closing desktop windows");
					DesktopUtils.CloseDesktopWindows(desktopName);
				});
			}

			WinApi.CloseHandle(jobProcess.processInfo.hThread);
			WinApi.WaitForSingleObject(jobProcess.processInfo.hProcess, WinApi.INFINITE);

			Cleanup(cleanupActions);

			return 0;
		}
	}
}
