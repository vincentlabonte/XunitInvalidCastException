using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Runners;

namespace Runner
{
    public class Runner
    {
        public Runner()
        {
            consoleLock = new object();
            finished = new ManualResetEvent(false);
            result = 0;
        }

        public async Task<int> RunAsync()
        {
            var testAssembly = @"C:\repos\XunitInvalidCastException\Test\bin\Debug\Test.dll";

            AppDomain.CurrentDomain.AssemblyResolve += delegate (object sender, ResolveEventArgs e)
            {
                String currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                AssemblyName requestedName = new AssemblyName(e.Name);
                if (requestedName.Name == "xunit.abstractions" || requestedName.Name == "xunit.runner.utility.net452")
                {
                    var resolvedAssembly = Assembly.LoadFrom(Path.Combine(currentFolder, requestedName.Name + ".dll"));
                    return resolvedAssembly;
                }
                return null;
            };

            using (var runner = AssemblyRunner.WithAppDomain(testAssembly))
            {
                runner.OnDiscoveryComplete = OnDiscoveryComplete;
                runner.OnExecutionComplete = OnExecutionComplete;
                runner.OnTestFailed = OnTestFailed;
                runner.OnTestSkipped = OnTestSkipped;

                Console.WriteLine("Discovering...");
                runner.Start();

                finished.WaitOne();
                finished.Dispose();

                return result;
            }
        }

        private void OnDiscoveryComplete(DiscoveryCompleteInfo info)
        {
            lock (consoleLock)
                Console.WriteLine($"Running {info.TestCasesToRun} of {info.TestCasesDiscovered} tests...");
        }

        private void OnExecutionComplete(ExecutionCompleteInfo info)
        {
            lock (consoleLock)
                Console.WriteLine($"Finished: {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");

            finished.Set();
        }

        private void OnTestFailed(TestFailedInfo info)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("[FAIL] {0}: {1}", info.TestDisplayName, info.ExceptionMessage);
                if (info.ExceptionStackTrace != null)
                    Console.WriteLine(info.ExceptionStackTrace);

                Console.ResetColor();
            }

            result = 1;
        }

        private void OnTestSkipped(TestSkippedInfo info)
        {
            lock (consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[SKIP] {0}: {1}", info.TestDisplayName, info.SkipReason);
                Console.ResetColor();
            }
        }

        // We use consoleLock because messages can arrive in parallel, so we want to make sure we get
        // consistent console output.
        private object consoleLock { get; }

        // Use an event to know when we're done
        private ManualResetEvent finished { get; }

        // Start out assuming success; we'll set this to 1 if we get a failed test
        private int result { get; set; }
    }
}
