using System;

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

            try
            {
                if (ReportCompilerUtility.ParseArguments(args))
                {
                    ReportCompilerUtility.Execute();
                }
            }
            catch (Exception ex)
            {
                ReportCompilerUtility.ShowUsage();
                ReportCompilerUtility.LogError(ex);
                Environment.ExitCode = 1;
            }
        }
    }
}
