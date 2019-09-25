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
            var argMap = Args.Parse(cmdArgs, new HashSet<string> { "driver" });
            var cleanupActions = new List<Action>();
            
            WriteLine("Headless driver");
            WriteLine(string.Join(", ", cmdArgs));

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

            CancelKeyPress += (s, ev) =>
            {
                WriteLine("Ctrl+C pressed");
                Cleanup(cleanupActions);
                ev.Cancel = true;
            };

            // TODO get desktop name from params
            var desktopName = "Headless-abcd";
            var desktopHandle = DesktopUtils.CreateDesktop(desktopName);
            WriteLine($"INFO: opened desktop {desktopHandle}");
            
            cleanupActions.Add(() =>
            {
                WriteLine($"INFO: closing desktop {desktopHandle}");
                DesktopUtils.CloseDesktop(desktopHandle);
            });

            var commandLine = String.Join(" ", argMap.Reminder);
            WriteLine($"INFO: Starting driver with cmdline '{commandLine}'");
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
            cleanupActions.Add(() =>
            {
                WriteLine("INFO: closing desktop windows");
                DesktopUtils.CloseDesktopWindows(desktopName);
            });

            WinApi.CloseHandle(jobProcess.processInfo.hThread);
            WinApi.WaitForSingleObject(jobProcess.processInfo.hProcess, WinApi.INFINITE);

            Cleanup(cleanupActions);

            return 0;
        }
    }
}
