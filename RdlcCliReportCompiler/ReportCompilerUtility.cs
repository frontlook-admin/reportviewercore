
using CliReportCompiler.ReportForm;
using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using FrontLookCoreLibraryAssembly.FL_GlobalClasses;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CliReportCompiler
{
    /// <summary>
    /// Utility class for RDLC report compilation and processing
    /// </summary>
    public static class ReportCompilerUtility
    {
        // Create bidirectional lookup for parameter names for faster access
        private static readonly Dictionary<string, string> ParameterNames = new()
        {
            { "ReportPath", "rp" },
            { "ReportDataSource", "ds" },
            { "ReportName", "rn" },
            { "Mode", "m" },
            { "ExportFormat", "ef" },
            { "ExportPath", "ep" },
            { "PrintSetupFile", "psf" },
            { "Test", "t" }
        };

        // Create reverse lookup for faster parameter normalization
        private static readonly Dictionary<string, string> ParameterShortToLong = new(StringComparer.OrdinalIgnoreCase);

        // Use regular Dictionary since thread-safety isn't required
        private static readonly Dictionary<string, string> GetParameters = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> RequiredParameters = new(StringComparer.OrdinalIgnoreCase)
        {
            "ReportPath", "ReportDataSource", "ReportName", "Mode", "PrintSetupFile"
        };

        // Static constructor to initialize the reverse lookup
        static ReportCompilerUtility()
        {
            foreach (var pair in ParameterNames)
            {
                ParameterShortToLong[pair.Value] = pair.Key;
            }
        }

        /// <summary>
        /// Shows usage information for the report compiler
        /// </summary>
        public static void ShowUsage()
        {
            @"Usage: CliReportCompiler.exe options
Options:
  --ReportPath|-rp          Path to the RDLC report file.
  --ReportDataSource|-ds    Path to the data source file (should be in xml along with xml schema in single file).
  --ReportName|-rn          Name of the report.
  --Mode|-m                 Operation mode: Preview, Print, PrintSetup, or Export.
  --ExportFormat|-ef        Export format: PDF, EXCEL, EXCELOPENXML, WORD, WORDOPENXML, IMAGE, HTML4_0, HTML5, MHTML
  --ExportPath|-ep          Path where the exported file will be saved.
  --PrintSetupFile|-psf     Path to the print setup file(JsonFile).
  --Test|-t                 Test message (for debugging purposes).
  --Demo|-d                 Run a demo of the ReportViewer.
  --Help|-h                 Display this help message.

    Parameters              Pass the parameters in the data source file with table name RldcParameters.

Example:
  CliReportCompiler.exe --reportPath ""C:\path\to\report.rdlc"" --reportDataSource ""C:\path\to\data.xml"" --PrintSetupFile ""C:\path\to\printsetup.json""  --Parameters ""json Parameters"" --ReportName ""ReportName"" --Mode ""Preview"" --ExportFormat ""PDF"" --ExportPath ""C:\path\to\exported\file"" --test ""Msg""
  CliReportCompiler --help".FL_ConsoleWriteDebug();
        }

        /// <summary>
        /// Runs a demo of the ReportViewer
        /// </summary>
        public static void RunDemo()
        {
            Console.WriteLine("Running demo...");

            string demoDataPath = Path.Combine(Environment.CurrentDirectory, "DemoDataset.xml");
            string demoReportPath = Path.Combine(Environment.CurrentDirectory, "DemoReport.rdlc");

            if (!File.Exists(demoDataPath) || !File.Exists(demoReportPath))
            {
                Console.WriteLine("Demo files not found. Please ensure DemoDataset.xml and DemoReport.rdlc exist in the application directory.");
                return;
            }

            string xmlContent = File.ReadAllText(demoDataPath);
            var report = new FL_IRdlcReport()
            {
                DataTables = xmlContent.FL_CastXmlToDataSet(),
                ReportFile = demoReportPath,
                ReportName = "DemoReport.rdlc",
            };

            using var form = new ReportViewerForm(report);
            form.ShowDialog();
        }

        /// <summary>
        /// Parses command line arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>True if processing should continue, false if a special command was handled</returns>
        public static bool ParseArguments(string[] args)
        {
            if (args == null || args.Length == 0) return false;

            // Check for special commands with case-insensitive comparison
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "--HELP", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-H", StringComparison.OrdinalIgnoreCase))
                {
                    ShowUsage();
                    return false;
                }

                if (string.Equals(arg, "--DEMO", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "-D", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "DEMO", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "D", StringComparison.OrdinalIgnoreCase))
                {
                    RunDemo();
                    return false;
                }
            }

            if (args.Length % 2 != 0)
            {
                Console.WriteLine("Invalid number of arguments.");
                ShowUsage();
                throw new ArgumentException("Invalid number of arguments. Expected key-value pairs.");
            }

            // Clear existing parameters before parsing new ones
            GetParameters.Clear();

            for (int i = 0; i < args.Length; i += 2)
            {
                string key = NormalizeParameterKey(args[i]);
                if (!string.IsNullOrEmpty(key))
                {
                    string value = args[i + 1].Trim('"');
#if DEBUG
                    value.FL_ConsoleWriteDebug();
#endif
                    GetParameters[key] = value;
                }
            }

            return true;
        }

        /// <summary>
        /// Normalizes parameter keys by removing prefixes and finding proper parameter names
        /// </summary>
        private static string NormalizeParameterKey(string key)
        {
            string cleanKey = key.TrimStart('-').TrimStart('-').Trim('"');
#if DEBUG
            cleanKey.FL_ConsoleWriteDebug();
#endif
            // First check if it's a long parameter name
            if (ParameterNames.ContainsKey(cleanKey))
            {
                return cleanKey;
            }

            // Then check if it's a short parameter name
            if (ParameterShortToLong.TryGetValue(cleanKey, out string longName))
            {
                return longName;
            }

            return string.Empty;
        }

        /// <summary>
        /// Interactive console-based argument parser
        /// </summary>
        public static void ParseArgumentsInteractively()
        {
            ShowUsage();
            Console.WriteLine("Type 'exit' and press enter to close the console.");

            while (true)
            {
                Console.WriteLine("\nEnter the arguments:");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                try
                {
                    string[] args = SplitCommandLineArgs(input);
                    if (ParseArguments(args))
                    {
                        Execute();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Splits command line arguments handling quoted values
        /// </summary>
        private static string[] SplitCommandLineArgs(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
                return Array.Empty<string>();

            var inQuotes = false;
            var parts = new List<string>(8); // Pre-allocate reasonable capacity
            var currentPart = new StringBuilder(50); // Pre-allocate reasonable capacity

            foreach (char c in commandLine)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                }
                else
                {
                    currentPart.Append(c);
                }
            }

            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Executes report processing based on the parsed arguments
        /// </summary>
        public static void Execute()
        {
            if (GetParameters.TryGetValue("Test", out string testValue) && !string.IsNullOrEmpty(testValue))
            {
                Console.WriteLine($"Test: {testValue}");
                // Use Console instead of MessageBox for CLI application
            }

            // Check for missing required parameters
            var missingParams = RequiredParameters.Where(p => !GetParameters.ContainsKey(p)).ToList();

            if (missingParams.Count > 0)
            {
                ShowUsage();
                StringBuilder exceptionMsg = new StringBuilder("Missing required parameters: ");
                exceptionMsg.AppendLine(string.Join(", ", missingParams));
                throw new ArgumentException(exceptionMsg.ToString());
            }

            ProcessReport();
        }

        /// <summary>
        /// Processes the report based on the parsed parameters
        /// </summary>
        public static void ProcessReport()
        {
            if (!GetParameters.TryGetValue("ReportPath", out string reportPath) ||
                !GetParameters.TryGetValue("ReportDataSource", out string dsFile) ||
                !GetParameters.TryGetValue("ReportName", out string reportName) ||
                !GetParameters.TryGetValue("Mode", out string mode) ||
                !GetParameters.TryGetValue("PrintSetupFile", out string printSetupFile))
            {
                throw new ArgumentException("Missing one or more required parameters.");
            }

            // Validate files exist
            ValidateFilesExist(reportPath, dsFile);

            // Create report compiler instance and configure it
            FL_IRdlcReport rldcReportCompiler = null;

            try
            {
                // Load data
                DataSet ds = LoadDataset(dsFile);

                // Create report compiler
                rldcReportCompiler = new FL_IRdlcReport()
                {
                    DataTables = ds,
                    ReportFile = reportPath,
                    ReportName = reportName,
                    PrintSettingFilePath = printSetupFile
                };

                // Load parameters and setup print settings
                LoadReportParameters(rldcReportCompiler, ds);
                SetupPrintSettings(rldcReportCompiler, printSetupFile, mode);

                // Process according to mode (using string constants to improve readability)
                const string PREVIEW = "PREVIEW";
                const string PRINTSETUP = "PRINTSETUP";
                const string PRINT = "PRINT";
                const string EXPORT = "EXPORT";

                string upperMode = mode.ToUpperInvariant();

                switch (upperMode)
                {
                    case PREVIEW:
                    case PRINTSETUP:
                        rldcReportCompiler.AltTriggerPrintSettings = string.Equals(mode, "PrintSetup", StringComparison.OrdinalIgnoreCase);
                        PreviewReport(rldcReportCompiler);
                        break;
                    case PRINT:
                        PrintReport(rldcReportCompiler);
                        break;
                    case EXPORT:
                        ExportReport(rldcReportCompiler);
                        break;
                    default:
                        throw new ArgumentException($"Invalid mode: {mode}. Supported modes: Preview, Print, PrintSetup, Export");
                }
            }
            catch (Exception ex)
            {
                rldcReportCompiler?.Dispose();
                throw new Exception($"Error processing report: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that required files exist
        /// </summary>
        private static void ValidateFilesExist(string reportPath, string dsFile)
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException("Report file not found", reportPath);
            }

            if (!File.Exists(dsFile))
            {
                throw new FileNotFoundException("Data source file not found", dsFile);
            }
        }

        /// <summary>
        /// Loads dataset from XML file
        /// </summary>
        private static DataSet LoadDataset(string dsFile)
        {
            try
            {
                string xmlContent = File.ReadAllText(dsFile);
                DataSet ds = xmlContent.FL_CastXmlToDataSet();

                if (ds.Tables.Count == 0)
                {
                    throw new Exception("No data tables found in the data source");
                }

                return ds;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading data source: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads report parameters from the dataset
        /// </summary>
        private static void LoadReportParameters(FL_IRdlcReport report, DataSet ds)
        {
            const string PARAMS_TABLE = "RldcParameters";

            if (ds.Tables.Contains(PARAMS_TABLE) && ds.Tables[PARAMS_TABLE].Rows.Count > 0)
            {
                DataTable paramsTable = ds.Tables[PARAMS_TABLE];
                report.ReportParameters = new List<FL_RdlcReportParameter>(paramsTable.Rows.Count);

                foreach (DataRow row in paramsTable.Rows)
                {
                    report.ReportParameters.Add(new FL_RdlcReportParameter
                    {
                        Name = row["Name"]?.ToString(),
                        Value = row["Value"]
                    });
                }
            }
        }

        /// <summary>
        /// Sets up print settings from file
        /// </summary>
        private static void SetupPrintSettings(FL_IRdlcReport report, string printSetupFile, string mode)
        {
            string setupDir = Path.GetDirectoryName(printSetupFile);

            if (!string.IsNullOrEmpty(setupDir) && !Directory.Exists(setupDir))
            {
                Directory.CreateDirectory(setupDir);
            }

            if (File.Exists(printSetupFile))
            {
                string fileContent = File.ReadAllText(printSetupFile);
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    var pageSettings = fileContent.FL_CastToClass<CustomPrintDialog>();
                    if (pageSettings?.CPageSettings != null)
                    {
                        report.PrintSettings = pageSettings;
                    }
                    else if (!string.Equals(mode, "PrintSetup", StringComparison.OrdinalIgnoreCase) &&
                             !string.Equals(mode, "Preview", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Invalid print settings file format");
                    }
                }
            }
        }

        /// <summary>
        /// Previews a report using the ReportViewer form
        /// </summary>
        /// <param name="report">The report to preview</param>
        public static void PreviewReport(FL_IRdlcReport report)
        {
            using var form = new ReportForm.FL_RdlcReportViewerForm(report);
            form.ShowDialog();
        }

        /// <summary>
        /// Gets the export format from the parameters
        /// </summary>
        /// <returns>The export format enumeration value</returns>
        public static ExportFormat GetExportFormat()
        {
            if (!GetParameters.TryGetValue("ExportFormat", out string formatParam))
            {
                throw new ArgumentException("ExportFormat parameter is required for export operations");
            }

            if (string.IsNullOrEmpty(formatParam))
            {
                throw new ArgumentException("Export format cannot be empty");
            }

            return formatParam.ToUpperInvariant() switch
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
                _ => throw new ArgumentException($"Unsupported export format: {formatParam}")
            };
        }

        /// <summary>
        /// Exports a report to the specified format
        /// </summary>
        /// <param name="report">The report to export</param>
        public static void ExportReport(FL_IRdlcReport report)
        {
            if (!GetParameters.TryGetValue("ExportFormat", out _))
            {
                throw new ArgumentException("ExportFormat is required for export operations");
            }

            if (!GetParameters.TryGetValue("ExportPath", out string exportPath))
            {
                throw new ArgumentException("ExportPath is required for export operations");
            }

            report.ExportFileName = exportPath;

            try
            {
                string exportDir = Path.GetDirectoryName(report.ExportFileName);
                if (!string.IsNullOrEmpty(exportDir) && !Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }

                report.Export();
            }
            finally
            {
                report?.Dispose();
            }
        }

        /// <summary>
        /// Prints a report using the LocalReport class
        /// </summary>
        /// <param name="report">The report to print</param>
        public static void PrintReport(FL_IRdlcReport report)
        {
            report.TriggerPrint = true;
            using var form = new ReportForm.FL_RdlcReportViewerForm(report);
            form.Print();
        }

        /// <summary>
        /// Gets a copy of the current parameter dictionary
        /// </summary>
        /// <returns>Dictionary of parameters</returns>
        public static Dictionary<string, string> GetCurrentParameters()
        {
            return new Dictionary<string, string>(GetParameters, StringComparer.OrdinalIgnoreCase);
        }
    }
}
