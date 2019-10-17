using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;

namespace headless
{
	static class Program
	{
		static string FindApplication(string driver)
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
			var argMap = Args.Parse(cmdArgs, new HashSet<string> { "app", "desktop" });

			if(cmdArgs.Length == 0) {
				WriteLine("Usage: headless <appname> [--<option-name> <option-value...] -- <app args>");
				WriteLine("Available options are:");
				WriteLine("  --app <appname> - application to start. Alternative way to set an app.");
				WriteLine("  --desktop - desktop name. Leave unspecified for random name.");
				WriteLine("  --desktop=false - do not use virtual desktop");
				return 1;
			}
			
			WriteLine($"Starting headless '{string.Join(" ", cmdArgs)}'");

			var appName =
				(argMap.Named.TryGetValue("app", out var val) ? val
					: System.Environment.GetEnvironmentVariable("HEADLESS_APP")) ??
					(argMap.Positional.Count > 0 ? argMap.Positional[0] : null);
		
			var appPath = FindApplication(appName);
			if (appPath == null)
			{
				WriteLine($"ERR: application '{appName}' not found'");
				return 1;
			}
			WriteLine($"INFO: starting application {appPath}");

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

			var cleanupActions = new List<Action>();

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
			WriteLine($"INFO: Starting app with cmdline '{commandLine}'");

			// TODO desktop default
			var jobProcess = DesktopUtils.CreateProcessInTheJob(desktopName, appPath, commandLine, IntPtr.Zero);
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
					Console.Beep();
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

			WriteLine($"INFO: app process id '{System.Diagnostics.Process.GetCurrentProcess().Id}'");

			WinApi.CloseHandle(jobProcess.processInfo.hThread);
			WinApi.WaitForSingleObject(jobProcess.processInfo.hProcess, WinApi.INFINITE);

			Cleanup(cleanupActions);

			return 0;
		}
	}
}
