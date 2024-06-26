using FastReport.Data;
using FrontLookCoreDbAccessLibrary.Desktop.FL_RDLC;
using FrontLookCoreDbAccessLibrary.FL_FastReport;
using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.Design.AxImporter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace CliReportCompiler
{
	/*
	usage example:
	CliReportCompiler.exe --reportPath|-rp "C:\path\to\report.rdlc" --reportDataSource|-ds "C:\path\to\data.xml" --Parameters|-p "json Parameters" --ReportName|-rn "ReportName" --Mode|-m "Preview|Print|Export" --ExportFormat|-ef "PDF|Excel|Word|Image" --ExportPath|-ep "C:\path\to\exported\file" --test|-t "Msg"
	*/





	class Program
	{

        static Dictionary<string, string> ParameterNames = new()
        {
            { "ReportPath", "rp" },
            { "ReportDataSource", "ds" },
            //{ "Parameters", "p" },
            { "ReportName", "rn" },
            { "Mode", "m" },
            { "ExportFormat", "ef" },
            { "ExportPath", "ep" },
            { "Test", "t" }
        };


        static void ShowUsage()
        {
            @"Usage: CliReportCompiler.exe options
Options:
  --ReportPath|-rp      Path to the RDLC report file.
  --ReportDataSource|-ds Path to the data source file (should be in xml along with xml schema in single file).
  --ReportName|-rn      Name of the report.
  --Mode|-m             Operation mode: Preview, Print, or Export.
  --ExportFormat|-ef    Export format: PDF, Excel, Word, or Image.
  --ExportPath|-ep      Path where the exported file will be saved.
  --Test|-t             Test message (for debugging purposes).
  --Demo|-d                Run a demo of the ReportViewer.
  --Help|-h             Display this help message.


    Parameters          Pass the parameters in the data source file with table name RldcParameters.

Example:
  CliReportCompiler.exe --reportPath ""C:\path\to\report.rdlc"" --reportDataSource ""C:\path\to\data.xml"" --Parameters ""json Parameters"" --ReportName ""ReportName"" --Mode ""Preview"" --ExportFormat ""PDF"" --ExportPath ""C:\path\to\exported\file"" --test ""Msg""
  CliReportCompiler --help".FL_ConsoleWriteDebug();
        }


        static void RunDemo()
        {
            // Here, you would create and show your form with the ReportViewer control.
            // This is a placeholder for the actual demo implementation.
            Console.WriteLine("Running demo...");
            using var form = new ReportViewerForm(); // Ensure you have a form named ReportViewerForm with a ReportViewer control.
            form.ShowDialog();
        }

        static Dictionary<string, string> GetParameters = new();
        static void ParseArguments(string[] args)
        {

            if (Array.Exists(args, arg => arg.ToUpper() == "--HELP" || arg.ToUpper() == "-H"))
            {
                ShowUsage();
                return;
            }
            // Run the demo if the --demo option is specified.
            if (Array.Exists(args, arg => arg.ToUpper() == "--DEMO" || arg.ToUpper() == "-D" || arg.ToUpper() == "DEMO" || arg.ToUpper() == "D"))
            {
                RunDemo();
                return;
            }
            if (args.Length % 2 != 0)
            {
                Console.WriteLine("Invalid number of arguments.");
                ShowUsage();
                args = null;
                throw new ArgumentException("Invalid number of arguments.");
            }
            GetParameters = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2)
            {
                //each argument should have a value and key contains the argument name. in key it contains -- or - as prefix. remove it

                //it should be in the format --key|-key value
#if DEBUG

                var _key = args[i].TrimStart('-', '-').TrimStart('"').TrimEnd('"').FL_ConsoleWriteDebug();
                var key = ParameterNames.FirstOrDefault(x => x.Value.ToUpper() == _key.ToUpper() || x.Key.ToUpper() == _key.ToUpper()).Key;

                var value = args[i + 1].TrimStart('"').TrimEnd('"').FL_ConsoleWriteDebug();

#else

                var _key = args[i].TrimStart('-', '-').FL_ConsoleWriteDebug();
                var key = ParameterNames.FirstOrDefault(x => x.Value.ToUpper() == _key.ToUpper() || x.Key.ToUpper() == _key.ToUpper()).Key;

                var value = args[i + 1];
#endif

                GetParameters.Add(key, value);
            }
        }

        //Console read line and parse the arguments
        private static void ParseArguments()
        {

            ShowUsage();
            //open console and wait for input
            Console.WriteLine("Type 'exit' and press enter to close the console.");
            Console.WriteLine("Enter the arguments:");
            var args = Console.ReadLine().Split(' ');

            if (args.Length == 1 && args[0] == "exit")
            {
                return;
            }

            try
            {
                ParseArguments(args);
                Execute();
                ParseArguments();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ParseArguments();
            }
        }

        [STAThread]
		static void Main(string[] args)
		{
            if(args.Length == 0)
            {
                ParseArguments();
            }
            else
            {
                if (Array.Exists(args, arg => arg.ToUpper() == "--HELP" || arg.ToUpper() == "-H"))
                {
                    ShowUsage();
                    return;
                }
                // Run the demo if the --demo option is specified.
                if (Array.Exists(args, arg => arg.ToUpper() == "--DEMO" || arg.ToUpper() == "-D"))
                {
                    RunDemo();
                    return;
                }
                try
                {
                    ParseArguments(args);
                    Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
        }

        static void Execute()
        {
            if(GetParameters.ContainsKey("Test"))
            {
                MessageBox.Show(GetParameters["Test"]);
            }

            if (!GetParameters.ContainsKey("ReportPath") || !GetParameters.ContainsKey("ReportDataSource") || !GetParameters.ContainsKey("ReportName") || !GetParameters.ContainsKey("Mode"))
            {
                //Console.WriteLine("Invalid number of arguments.");
                ShowUsage();
                throw new ArgumentException("Invalid arguments.");
            }
            else
            {
                ProcessReport();
            }
        }

        static void ProcessReport()
        {
            //--ReportPath "G:\Repos\frontlook-admin\AccLead\AccLead.Desktop\bin\Debug\net8.0-windows\ReportTemplates\Reports\RDLC\FinalAccount\TrialBalanceReport\TrialBalanceReport.rdlc" --ReportDataSource "C:\Users\deban\AppData\Local\Temp\tmp3kczp5.tmp" --ReportName "TrialBalanceReport.rdlc" --Mode "Preview"

            //if the below parameters are not passed, then throw an exception
            if (!GetParameters.ContainsKey("ReportPath") || !GetParameters.ContainsKey("ReportDataSource") || !GetParameters.ContainsKey("ReportName") || !GetParameters.ContainsKey("Mode"))
            {
                throw new ArgumentException("Invalid arguments.");
            }

            var reportPath = GetParameters["ReportPath"];
            var dsFile = GetParameters["ReportDataSource"];
            var ds = File.ReadAllText(dsFile).FL_CastXmlToDataSet();
            
            var reportName = GetParameters["ReportName"];
            var mode = GetParameters["Mode"];


            var rldcReportCompiler = new FL_IRdlcReport()
            {
                DataTables = ds,
                ReportFile = reportPath,
                ReportName = reportName
            };

            if(ds.Tables.Count == 0)
            {
                throw new Exception("No data to load");
            }

            if (string.IsNullOrEmpty(reportPath))
            {
                throw new Exception("No report file to load");
            }

            if (!File.Exists(reportPath))
            {
                throw new Exception("Report file not found");
            }

            if (ds.Tables.Cast<DataTable>().Any(x => x.TableName == "RldcParameters"))
            {
                //load the parameters
                var ParametersCount = ds.Tables["RldcParameters"].Rows.Count;
                if (ParametersCount > 0)
                {
                    //load the parameters in rlcdReportCompiler
                    rldcReportCompiler.ReportParameters = new List<FL_RldcReportParameter>();

                    for (int i = 0; i < ParametersCount; i++)
                    {
                        rldcReportCompiler.ReportParameters.Add(new FL_RldcReportParameter()
                        {
                            Name = ds.Tables["RldcParameters"].Rows[i]["Name"].ToString(),
                            Value = ds.Tables["RldcParameters"].Rows[i]["Value"]
                        });
                    }
                }
            }


            if (mode == "Preview")
            {
                rldcReportCompiler.Preview();
            }
            else if (mode == "Print")
            {
                PrintReport();
            }
            else if (mode == "Export")
            {
                ExportReport();
            }
        }

        static void PreviewReport()
        {

        }
        static void ExportReport()
        {
            var exportFormat = GetParameters["ExportFormat"];
            var exportPath = GetParameters["ExportPath"];
            var test = GetParameters["test"];

            throw new NotImplementedException();
        }
        static void PrintReport()
        {
            var exportPath = GetParameters["ExportPath"];
            var test = GetParameters["test"];

            throw new NotImplementedException();
        }

    }

}
