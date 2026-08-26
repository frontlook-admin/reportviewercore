using System;
using System.Threading;

namespace CliReportCompiler
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ReportCompilerUtility.ParseArgumentsInteractively();
                return;
            }

            using var cancellationSource = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellationSource.Cancel();
            };
            Console.CancelKeyPress += cancelHandler;

            try
            {
                var runner = ReportCompilerRunner.TryParse(args);
                if (runner != null)
                {
                    runner.Execute(cancellationSource.Token);
                }
            }
            catch (Exception ex)
            {
                ReportCompilerUtility.ShowUsage();
                ReportCompilerUtility.LogError(ex);
                Environment.ExitCode = ReportCompilerUtility.GetExitCode(ex);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
    }
}
