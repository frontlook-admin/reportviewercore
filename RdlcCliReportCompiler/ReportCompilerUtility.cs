using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using FrontLookCoreLibraryAssembly.FL_GlobalClasses;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CliReportCompiler
{
    /// <summary>
    /// Parses CLI options and processes RDLC reports.
    /// </summary>
    public static class ReportCompilerUtility
    {
        private const string ParametersTableName = "RldcParameters";
        private const string AttachSubReportOption = "AttachSubReport";
        private const string PreviewMode = "PREVIEW";
        private const string PrintMode = "PRINT";
        private const string PrintSetupMode = "PRINTSETUP";
        private const string PrintSettingsMode = "PRINTSETTINGS";
        private const string ExportMode = "EXPORT";
        private const string WaitForViewerOption = "WaitForViewer";
        private const string EnableErrorLoggingOption = "EnableErrorLogging";
        private const string VerboseOption = "Verbose";
        private const string LogFileOption = "LogFile";
        private const string ContinueOnErrorOption = "ContinueOnError";
        private const string VersionOption = "Version";

        private const string UsageText = @"Usage: CliReportCompiler.exe options
Options:
  --ReportPath|-rp          Path to the RDLC report file.
  --ReportDataSource|-ds    Path to the data source file (should be in xml along with xml schema in single file).
  --ReportName|-rn          Name of the report.
  --Mode|-m                 Operation mode: Preview, Print, PrintSetup, PrintSettings, or Export.
  --ExportFormat|-ef        Export format: PDF, EXCEL, EXCELOPENXML, WORD, WORDOPENXML, IMAGE, HTML4_0, HTML5, MHTML
  --ExportPath|-ep          Path where the exported file will be saved.
  --AttachSubReport|-asr    Attach sub reports using 'key1=value1,key2=value2'.
  --PrintSetupFile|-psf     Path to the print setup file (JSON).
  --WaitForViewer|-wfv      Wait for the viewer to close: true or false. Default: true.
  --EnableErrorLogging|-el  Include detailed exception information in CLI error logs: true or false.
  --Verbose|-v              Write lifecycle details to the console and log file.
  --LogFile|-lf             Write structured JSONL logs to this file.
  --ContinueOnError|-coe    Continue interactive input after an error: true or false. Default: true.
  --Version|--ver|-ver      Display compiler and ReportViewer versions.
  --Test|-t                 Test message (for debugging purposes).
  --Demo|-d                 Run a demo of the ReportViewer.
  --Help|-h                 Display this help message.

Parameters are loaded from the data source file using the table name RldcParameters.

Example:
  CliReportCompiler.exe --ReportPath ""C:\path\to\report.rdlc"" --ReportDataSource ""C:\path\to\data.xml"" --PrintSetupFile ""C:\path\to\printsetup.json"" --ReportName ""ReportName"" --Mode ""Preview"" --ExportFormat ""PDF"" --ExportPath ""C:\path\to\exported\file"" --Test ""Msg""
  CliReportCompiler.exe --ReportPath ""C:\path\to\report.rdlc"" --ReportDataSource ""C:\path\to\data.xml"" --PrintSetupFile ""C:\path\to\printsetup.json"" --ReportName ""ReportName"" --Mode ""Preview"" --WaitForViewer false --EnableErrorLogging true
  CliReportCompiler.exe --Help";

        private static readonly IReadOnlyDictionary<string, string> ParameterNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReportPath"] = "rp",
                ["ReportDataSource"] = "ds",
                ["ReportName"] = "rn",
                ["Mode"] = "m",
                [AttachSubReportOption] = "asr",
                ["ExportFormat"] = "ef",
                ["ExportPath"] = "ep",
                ["PrintSetupFile"] = "psf",
                [WaitForViewerOption] = "wfv",
                [EnableErrorLoggingOption] = "el",
                [VerboseOption] = "v",
                [LogFileOption] = "lf",
                [ContinueOnErrorOption] = "coe",
                ["Test"] = "t"
            };

        private static readonly IReadOnlyDictionary<string, string> ParameterShortToLong =
            ParameterNames.ToDictionary(x => x.Value, x => x.Key, StringComparer.OrdinalIgnoreCase);

        private static readonly string[] RequiredParameters =
        {
            "ReportPath",
            "ReportDataSource",
            "ReportName",
            "Mode",
            "PrintSetupFile"
        };

        private static readonly Dictionary<string, string> GetParameters =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> GetSubReports =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly object ConsoleSync = new();

        /// <summary>
        /// Shows usage information for the report compiler.
        /// </summary>
        public static void ShowUsage()
        {
            lock (ConsoleSync)
            {
                Console.WriteLine(UsageText);
            }
        }

        /// <summary>
        /// Runs the bundled demo report.
        /// </summary>
        public static void RunDemo()
        {
            var demoDataPath = Path.Combine(Environment.CurrentDirectory, "DemoDataset.xml");
            var demoReportPath = Path.Combine(Environment.CurrentDirectory, "DemoReport.rdlc");

            if (!File.Exists(demoDataPath) || !File.Exists(demoReportPath))
            {
                Console.WriteLine("Demo files not found. Please ensure DemoDataset.xml and DemoReport.rdlc exist in the application directory.");
                return;
            }

            using var report = new FL_IRdlcReport
            {
                DataTables = LoadDataset(demoDataPath),
                ReportFile = demoReportPath,
                ReportName = "DemoReport.rdlc"
            };
            using var form = new ReportForm.FL_RdlcReportViewerForm(report);
            form.ShowDialog();
        }

        /// <summary>
        /// Parses command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        /// <returns><see langword="true"/> when report execution should continue.</returns>
        public static bool ParseArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                GetParameters.Clear();
                GetSubReports.Clear();
                return false;
            }

            GetParameters.Clear();
            GetSubReports.Clear();

            if (HasArgument(args, "--HELP", "-H"))
            {
                ShowUsage();
                return false;
            }

            if (HasArgument(args, $"--{VersionOption}", "--ver", "-ver"))
            {
                ShowVersion();
                return false;
            }

            if (HasArgument(args, "--DEMO", "-D", "DEMO", "D"))
            {
                RunDemo();
                return false;
            }

            var parsedParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var parsedSubReports = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++)
            {
                var parameterName = NormalizeParameterKey(args[i]);
                var value = "true";

                if (i + 1 >= args.Length || IsOptionToken(args[i + 1]))
                {
                    if (!IsBooleanOption(parameterName))
                    {
                        throw new ArgumentException($"Option {args[i]} requires a value.");
                    }
                }
                else
                {
                    value = args[++i].Trim().Trim('"');
                }

                if (string.Equals(parameterName, AttachSubReportOption, StringComparison.OrdinalIgnoreCase))
                {
                    ParseSubReports(value, parsedSubReports);
                    parsedParameters[AttachSubReportOption] = string.Join(",", parsedSubReports.Select(x => $"{x.Key}={x.Value}"));
                }
                else
                {
                    ValidateBooleanOption(parameterName, value);
                    parsedParameters[parameterName] = value;
                }
            }

            foreach (var parameter in parsedParameters)
            {
                GetParameters[parameter.Key] = parameter.Value;
            }

            foreach (var subReport in parsedSubReports)
            {
                GetSubReports[subReport.Key] = subReport.Value;
            }

            return true;
        }

        /// <summary>
        /// Interactive console-based argument parser.
        /// </summary>
        public static void ParseArgumentsInteractively()
        {
            while (true)
            {
                ShowInteractivePrompt();
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || string.Equals(input.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                try
                {
                    var args = SplitCommandLineArgs(input);
                    if (ParseArguments(args))
                    {
                        Execute();
                        Environment.ExitCode = 0;
                    }
                }
                catch (Exception ex)
                {
                    ShowUsage();
                    LogError(ex, GetErrorLoggingEnabled());
                    Environment.ExitCode = 1;

                    if (!GetContinueOnError())
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Displays the compiler and ReportViewer assembly versions.
        /// </summary>
        public static void ShowVersion()
        {
            var compilerVersion = typeof(ReportCompilerUtility).Assembly.GetName().Version?.ToString() ?? "unknown";
            var viewerVersion = typeof(ReportViewer).Assembly.GetName().Version?.ToString() ?? "unknown";
            Console.WriteLine($"CliReportCompiler {compilerVersion}");
            Console.WriteLine($"ReportViewer {viewerVersion}");
        }

        /// <summary>
        /// Splits an interactive command line while preserving quoted values.
        /// </summary>
        public static string[] SplitCommandLineArgs(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return Array.Empty<string>();
            }

            var parts = new List<string>();
            var currentPart = new StringBuilder();
            var inQuotes = false;

            foreach (var character in commandLine)
            {
                if (character == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(character) && !inQuotes)
                {
                    AddCurrentPart(parts, currentPart);
                    continue;
                }

                currentPart.Append(character);
            }

            if (inQuotes)
            {
                throw new ArgumentException("Unterminated quoted argument.");
            }

            AddCurrentPart(parts, currentPart);
            return parts.ToArray();
        }

        /// <summary>
        /// Executes report processing using the currently parsed parameters.
        /// </summary>
        public static void Execute()
        {
            var stopwatch = Stopwatch.StartNew();
            var reportPath = GetOptionalParameter("ReportPath");
            LogInfo("execution_started", reportPath);

            if (GetParameters.TryGetValue("Test", out var testValue) && !string.IsNullOrEmpty(testValue))
            {
                Console.WriteLine($"Test: {testValue}");
            }

            var missingParameters = RequiredParameters
                .Where(parameter => !GetParameters.ContainsKey(parameter))
                .ToArray();

            if (missingParameters.Length > 0)
            {
                throw new ArgumentException($"Missing required parameters: {string.Join(", ", missingParameters)}");
            }

            try
            {
                ProcessReport(GetWaitForViewer(), GetErrorLoggingEnabled());
                LogInfo("execution_succeeded", reportPath, stopwatch.Elapsed);
            }
            catch
            {
                LogInfo("execution_failed", reportPath, stopwatch.Elapsed);
                throw;
            }
        }

        /// <summary>
        /// Loads and processes the report using the selected mode.
        /// </summary>
        public static void ProcessReport()
        {
            ProcessReport(GetWaitForViewer(), GetErrorLoggingEnabled());
        }

        private static void ProcessReport(bool waitForViewer, bool enableErrorLogging)
        {
            var reportPath = GetRequiredParameter("ReportPath");
            var dataSourcePath = GetRequiredParameter("ReportDataSource");
            var reportName = GetRequiredParameter("ReportName");
            var mode = GetRequiredParameter("Mode");
            var printSetupFile = GetRequiredParameter("PrintSetupFile");
            var logFile = GetOptionalParameter(LogFileOption);

            LogInfo("report_started", reportPath, null, new { Mode = mode, ReportName = reportName });

            ValidateFilesExist(reportPath, dataSourcePath);
            var dataSet = LoadDataset(dataSourcePath);

            var report = new FL_IRdlcReport
            {
                DataTables = dataSet,
                ReportFile = reportPath,
                ReportName = reportName,
                PrintSettingFilePath = printSetupFile,
                SubReports = new Dictionary<string, string>(GetSubReports, StringComparer.OrdinalIgnoreCase)
            };
            var reportOwnershipTransferred = false;

            try
            {
                LoadReportParameters(report, dataSet);
                SetupPrintSettings(report, printSetupFile, mode);

                switch (mode.Trim().ToUpperInvariant())
                {
                    case PreviewMode:
                        PreviewReport(report, waitForViewer, enableErrorLogging, logFile);
                        reportOwnershipTransferred = !waitForViewer;
                        break;
                    case PrintSetupMode:
                    case PrintSettingsMode:
                        report.AltTriggerPrintSettings = true;
                        PreviewReport(report, waitForViewer, enableErrorLogging, logFile);
                        reportOwnershipTransferred = !waitForViewer;
                        break;
                    case PrintMode:
                        PrintReport(report);
                        break;
                    case ExportMode:
                        ExportReport(report);
                        break;
                    default:
                        throw new ArgumentException($"Invalid mode: {mode}. Supported modes: Preview, Print, PrintSetup, PrintSettings, Export");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing report: {ex.Message}", ex);
            }
            finally
            {
                if (!reportOwnershipTransferred)
                {
                    report.Dispose();
                }
            }
        }

        /// <summary>
        /// Validates that required input files exist.
        /// </summary>
        private static void ValidateFilesExist(string reportPath, string dataSourcePath)
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException("Report file not found", reportPath);
            }

            if (!File.Exists(dataSourcePath))
            {
                throw new FileNotFoundException("Data source file not found", dataSourcePath);
            }
        }

        /// <summary>
        /// Loads a dataset from the supplied XML file once.
        /// </summary>
        private static DataSet LoadDataset(string dataSourcePath)
        {
            try
            {
                var dataSet = File.ReadAllText(dataSourcePath).FL_CastXmlToDataSet();
                if (dataSet.Tables.Count == 0)
                {
                    throw new InvalidDataException("No data tables found in the data source");
                }

                return dataSet;
            }
            catch (Exception ex) when (ex is not InvalidDataException)
            {
                throw new InvalidDataException($"Error loading data source: {ex.Message}", ex);
            }
        }

        private static void LoadReportParameters(FL_IRdlcReport report, DataSet dataSet)
        {
            if (!dataSet.Tables.Contains(ParametersTableName))
            {
                return;
            }

            var parameterTable = dataSet.Tables[ParametersTableName];
            if (parameterTable.Rows.Count == 0)
            {
                return;
            }

            if (!parameterTable.Columns.Contains("Name") || !parameterTable.Columns.Contains("Value"))
            {
                throw new InvalidDataException($"The {ParametersTableName} table must contain Name and Value columns.");
            }

            report.ReportParameters = new List<FL_RdlcReportParameter>(parameterTable.Rows.Count);
            foreach (DataRow row in parameterTable.Rows)
            {
                report.ReportParameters.Add(new FL_RdlcReportParameter
                {
                    Name = row["Name"]?.ToString(),
                    Value = row["Value"]
                });
            }
        }

        private static void SetupPrintSettings(FL_IRdlcReport report, string printSetupFile, string mode)
        {
            if (string.IsNullOrWhiteSpace(printSetupFile))
            {
                throw new ArgumentException("PrintSetupFile cannot be empty.");
            }

            var setupDirectory = Path.GetDirectoryName(printSetupFile);
            if (!string.IsNullOrEmpty(setupDirectory))
            {
                Directory.CreateDirectory(setupDirectory);
            }

            if (!File.Exists(printSetupFile))
            {
                return;
            }

            var fileContent = File.ReadAllText(printSetupFile);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                return;
            }

            var pageSettings = fileContent.FL_CastToClass<CustomPrintDialog>();
            if (pageSettings?.CPageSettings != null)
            {
                report.PrintSettings = pageSettings;
                return;
            }

            var normalizedMode = mode.Trim().ToUpperInvariant();
            if (normalizedMode != PreviewMode && normalizedMode != PrintSetupMode && normalizedMode != PrintSettingsMode)
            {
                throw new InvalidDataException("Invalid print settings file format.");
            }
        }

        /// <summary>
        /// Shows a report in the canonical viewer form.
        /// </summary>
        public static void PreviewReport(FL_IRdlcReport report)
        {
            PreviewReport(report, waitForViewer: true, enableErrorLogging: false);
        }

        /// <summary>
        /// Shows a report and optionally returns immediately while the viewer remains open.
        /// </summary>
        /// <param name="report">Report to display.</param>
        /// <param name="waitForViewer">When true, waits until the viewer closes.</param>
        /// <param name="enableErrorLogging">When true, writes detailed viewer exceptions to the CLI.</param>
        public static void PreviewReport(FL_IRdlcReport report, bool waitForViewer, bool enableErrorLogging = false)
        {
            PreviewReport(report, waitForViewer, enableErrorLogging, GetOptionalParameter(LogFileOption));
        }

        private static void PreviewReport(FL_IRdlcReport report, bool waitForViewer, bool enableErrorLogging, string logFile)
        {
            ArgumentNullException.ThrowIfNull(report);

            if (waitForViewer)
            {
                using var form = new ReportForm.FL_RdlcReportViewerForm(report);
                form.ShowDialog();
                return;
            }

            LaunchViewer(report, enableErrorLogging, logFile);
        }

        /// <summary>
        /// Gets the configured export format.
        /// </summary>
        public static ExportFormat GetExportFormat()
        {
            var format = GetRequiredParameter("ExportFormat");
            return format.Trim().ToUpperInvariant() switch
            {
                "PDF" => ExportFormat.PDF,
                "EXCEL" => ExportFormat.EXCEL,
                "EXCELOPENXML" => ExportFormat.EXCELOPENXML,
                "WORD" => ExportFormat.WORD,
                "WORDOPENXML" => ExportFormat.WORDOPENXML,
                "IMAGE" => ExportFormat.IMAGE,
                "HTML4_0" => ExportFormat.HTML4_0,
                "HTML5" => ExportFormat.HTML5,
                "MHTML" => ExportFormat.MHTML,
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };
        }

        /// <summary>
        /// Exports a report to the configured path and format.
        /// </summary>
        public static void ExportReport(FL_IRdlcReport report)
        {
            var requestedExportPath = GetRequiredParameter("ExportPath");
            var exportPath = Path.GetFullPath(requestedExportPath);
            report.ExportFormat = GetExportFormat();

            var exportDirectory = Path.GetDirectoryName(exportPath);
            if (string.IsNullOrEmpty(exportDirectory))
            {
                throw new ArgumentException("ExportPath must include a valid output file name.");
            }

            Directory.CreateDirectory(exportDirectory);
            var temporaryExportPath = Path.Combine(
                exportDirectory,
                $".{Path.GetFileName(exportPath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                report.ExportFileName = temporaryExportPath;
                LogInfo("export_started", exportPath, null, new { Format = report.ExportFormat.ToString() });
                report.Export();

                if (!File.Exists(temporaryExportPath))
                {
                    throw new IOException($"Export did not create an output file: {temporaryExportPath}");
                }

                var fileInfo = new FileInfo(temporaryExportPath);
                if (fileInfo.Length == 0)
                {
                    throw new IOException("Export created an empty output file.");
                }

                File.Move(temporaryExportPath, exportPath, overwrite: true);
                report.ExportFileName = exportPath;
                LogInfo("export_succeeded", exportPath, null, new { Bytes = fileInfo.Length });
            }
            finally
            {
                if (File.Exists(temporaryExportPath))
                {
                    File.Delete(temporaryExportPath);
                }
            }
        }

        /// <summary>
        /// Prints a report using the canonical viewer form.
        /// </summary>
        public static void PrintReport(FL_IRdlcReport report)
        {
            report.TriggerPrint = true;
            using var form = new ReportForm.FL_RdlcReportViewerForm(report);
            form.Print();
        }

        /// <summary>
        /// Gets a copy of the currently parsed parameters.
        /// </summary>
        public static Dictionary<string, string> GetCurrentParameters()
        {
            return new Dictionary<string, string>(GetParameters, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Writes a CLI error using the currently configured logging option.
        /// </summary>
        public static void LogError(Exception exception)
        {
            LogError(exception, GetErrorLoggingEnabled());
        }

        private static void LaunchViewer(FL_IRdlcReport report, bool enableErrorLogging, string logFile)
        {
            var viewerReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var viewerThread = new Thread(() => RunViewer(report, enableErrorLogging, logFile, viewerReady))
            {
                IsBackground = false,
                Name = $"RDLC viewer: {report.ReportName}"
            };
            viewerThread.SetApartmentState(ApartmentState.STA);
            viewerThread.Start();
            viewerReady.Task.GetAwaiter().GetResult();
        }

        private static void RunViewer(
            FL_IRdlcReport report,
            bool enableErrorLogging,
            string logFile,
            TaskCompletionSource<bool> viewerReady)
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                using var form = new ReportForm.FL_RdlcReportViewerForm(report);
                form.Shown += (_, _) => viewerReady.TrySetResult(true);
                Application.Run(form);
            }
            catch (Exception ex)
            {
                LogError(ex, enableErrorLogging, report.ReportFile, logFile);
            }
            finally
            {
                report.Dispose();
                viewerReady.TrySetResult(true);
            }
        }

        private static bool HasArgument(IEnumerable<string> args, params string[] expectedArguments)
        {
            return args.Any(argument => expectedArguments.Any(expected =>
                string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase)));
        }

        private static string NormalizeParameterKey(string key)
        {
            var cleanKey = key.Trim().Trim('"').TrimStart('-');

            if (ParameterNames.ContainsKey(cleanKey))
            {
                return cleanKey;
            }

            if (ParameterShortToLong.TryGetValue(cleanKey, out var longName))
            {
                return longName;
            }

            throw new ArgumentException($"Unknown option: {key}");
        }

        private static bool IsOptionToken(string value)
        {
            return value.TrimStart().StartsWith("-", StringComparison.Ordinal);
        }

        private static bool IsBooleanOption(string parameterName)
        {
            return string.Equals(parameterName, WaitForViewerOption, StringComparison.OrdinalIgnoreCase)
                || string.Equals(parameterName, EnableErrorLoggingOption, StringComparison.OrdinalIgnoreCase)
                || string.Equals(parameterName, VerboseOption, StringComparison.OrdinalIgnoreCase)
                || string.Equals(parameterName, ContinueOnErrorOption, StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateBooleanOption(string parameterName, string value)
        {
            if (IsBooleanOption(parameterName) && !TryParseBoolean(value, out _))
            {
                throw new ArgumentException($"{parameterName} must be true or false.");
            }
        }

        private static bool GetWaitForViewer()
        {
            return GetBooleanParameter(WaitForViewerOption, defaultValue: true);
        }

        private static bool GetErrorLoggingEnabled()
        {
            return GetBooleanParameter(EnableErrorLoggingOption, defaultValue: false);
        }

        private static bool GetVerboseLoggingEnabled()
        {
            return GetBooleanParameter(VerboseOption, defaultValue: false);
        }

        private static bool GetContinueOnError()
        {
            return GetBooleanParameter(ContinueOnErrorOption, defaultValue: true);
        }

        private static string GetLogFilePath()
        {
            return GetOptionalParameter(LogFileOption);
        }

        private static bool GetBooleanParameter(string parameterName, bool defaultValue)
        {
            if (!GetParameters.TryGetValue(parameterName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (TryParseBoolean(value, out var result))
            {
                return result;
            }

            throw new ArgumentException($"{parameterName} must be true or false.");
        }

        private static bool TryParseBoolean(string value, out bool result)
        {
            if (bool.TryParse(value, out result))
            {
                return true;
            }

            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        private static void ShowInteractivePrompt()
        {
            lock (ConsoleSync)
            {
                ShowUsage();
                Console.WriteLine("Type 'exit' and press enter to close the console.");
                Console.WriteLine("Enter the arguments:");
            }
        }

        private static void LogInfo(string eventName, string reportPath, TimeSpan? duration = null, object details = null)
        {
            var logFile = GetLogFilePath();
            if (!GetVerboseLoggingEnabled() && string.IsNullOrWhiteSpace(logFile))
            {
                return;
            }

            var message = eventName;
            if (GetVerboseLoggingEnabled())
            {
                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    message += $" report='{reportPath}'";
                }

                if (duration.HasValue)
                {
                    message += $" durationMs={duration.Value.TotalMilliseconds:0}";
                }

                lock (ConsoleSync)
                {
                    Console.WriteLine($"[Info] {message}");
                }
            }

            WriteStructuredLog("Info", eventName, reportPath, message, null, duration, details, logFile);
        }

        private static void LogError(Exception exception, bool enableErrorLogging, string reportPath = null, string logFile = null)
        {
            var context = string.IsNullOrWhiteSpace(reportPath)
                ? string.Empty
                : $" for report '{reportPath}'";

            logFile ??= GetLogFilePath();

            lock (ConsoleSync)
            {
                if (enableErrorLogging)
                {
                    Console.Error.WriteLine($"Error{context}: {exception}");
                }
                else
                {
                    Console.Error.WriteLine($"Error{context}: {exception.Message}");
                }
            }

            WriteStructuredLog("Error", "error", reportPath, exception.Message, exception, null, null, logFile);
        }

        private static void WriteStructuredLog(
            string level,
            string eventName,
            string reportPath,
            string message,
            Exception exception,
            TimeSpan? duration,
            object details,
            string logFile)
        {
            if (string.IsNullOrWhiteSpace(logFile))
            {
                return;
            }

            try
            {
                var record = new Dictionary<string, object>
                {
                    ["timestamp"] = DateTimeOffset.UtcNow,
                    ["level"] = level,
                    ["event"] = eventName,
                    ["message"] = message
                };

                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    record["reportPath"] = reportPath;
                }

                if (duration.HasValue)
                {
                    record["durationMs"] = duration.Value.TotalMilliseconds;
                }

                if (details != null)
                {
                    record["details"] = details;
                }

                if (exception != null)
                {
                    record["exception"] = exception.ToString();
                }

                var fullLogFile = Path.GetFullPath(logFile);
                var directory = Path.GetDirectoryName(fullLogFile);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (ConsoleSync)
                {
                    File.AppendAllText(fullLogFile, JsonSerializer.Serialize(record) + Environment.NewLine);
                }
            }
            catch (Exception logException)
            {
                lock (ConsoleSync)
                {
                    Console.Error.WriteLine($"Unable to write log file '{logFile}': {logException.Message}");
                }
            }
        }

        private static void ParseSubReports(string value, IDictionary<string, string> subReports)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("AttachSubReport value is missing.");
            }

            foreach (var entry in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var separatorIndex = entry.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
                {
                    throw new ArgumentException($"AttachSubReport entry is invalid: {entry}");
                }

                var subReportName = entry[..separatorIndex].Trim();
                var subReportPath = entry[(separatorIndex + 1)..].Trim();

                if (subReportName.Length == 0 || subReportPath.Length == 0)
                {
                    throw new ArgumentException($"AttachSubReport entry is invalid: {entry}");
                }

                if (!File.Exists(subReportPath))
                {
                    throw new FileNotFoundException("Sub report file not found", subReportPath);
                }

                subReports[subReportName] = subReportPath;
            }
        }

        private static string GetRequiredParameter(string parameterName)
        {
            if (!GetParameters.TryGetValue(parameterName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} is required.");
            }

            return value;
        }

        private static string GetOptionalParameter(string parameterName)
        {
            return GetParameters.TryGetValue(parameterName, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : null;
        }

        private static void AddCurrentPart(ICollection<string> parts, StringBuilder currentPart)
        {
            if (currentPart.Length == 0)
            {
                return;
            }

            parts.Add(currentPart.ToString());
            currentPart.Clear();
        }
    }
}
