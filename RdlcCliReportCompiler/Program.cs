﻿
using CliReportCompiler.ReportForm;
using FastReport.Data;

using FrontLookCoreDbAccessLibrary.Desktop.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.Common.FrontLookCode;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using Namotion.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
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
            { "PrintSetupFile", "psf" },
            { "Test", "t" }
        };


        static void ShowUsage()
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
        /*--ReportPath "G:\Repos\frontlook-admin\AccLead\AccLead.Desktop\bin\Debug\net8.0-windows\ReportTemplates\Reports\RDLC\FinalAccount\TrialBalanceReport\TrialBalanceReport.rdlc" --ReportDataSource "C:\Users\deban\AppData\Local\Temp\tmp2t0lpk.tmp" --ReportName "TrialBalanceReport.rdlc" --PrintSetupFile "G:\Repos\frontlook-admin\AccLead\AccLead.Desktop\bin\Debug\net8.0-windows\CompanyReportSettings\1DCA0B1D-83F4AC6F-FC5A1698-C134895E-0C85BD79-B9E13A3D-4340A34D-B5B7EF0D\AccLead-2324\PrintSettings\TrialBalanceReport\TrialBalanceReport.txt" --Mode "Print"*/



        static void RunDemo()
        {
            // Here, you would create and show your form with the ReportViewer control.
            // This is a placeholder for the actual demo implementation.
            Console.WriteLine("Running demo...");

            var report = new FL_IRdlcReport()
            {
                DataTables = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "DemoDataset.xml")).FL_CastXmlToDataSet(),
                ReportFile = Path.Combine(Environment.CurrentDirectory, "DemoReport.rdlc"),
                ReportName = "DemoReport.rdlc",
                //PrintSettingFilePath = Path.Combine(Environment.CurrentDirectory, "DemoPrintSetup.json")
            };



            using var form = new ReportViewerForm(report); // Ensure you have a form named ReportViewerForm with a ReportViewer control.
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
                ParseArguments(args);
                Execute();
                GetParameters.Clear();
                ParseArguments();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ParseArguments();
            }
        }

        //PageSetupDialog()
        //PageSettings

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
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

            if (!GetParameters.ContainsKey("ReportPath") || !GetParameters.ContainsKey("ReportDataSource") || !GetParameters.ContainsKey("ReportName") || !GetParameters.ContainsKey("Mode") || !GetParameters.ContainsKey("PrintSetupFile"))
            {
                //Console.WriteLine("Invalid number of arguments.");
                ShowUsage();
                //throw exception showing the specific key that is missing
                StringBuilder exceptionMsg = new StringBuilder();
                if (!GetParameters.ContainsKey("ReportPath"))
                {
                    exceptionMsg.AppendLine("ReportPath is missing.");
                }
                if (!GetParameters.ContainsKey("ReportDataSource"))
                {
                    exceptionMsg.AppendLine("ReportDataSource is missing.");
                }
                if (!GetParameters.ContainsKey("ReportName"))
                {
                    exceptionMsg.AppendLine("ReportName is missing.");
                }
                if (!GetParameters.ContainsKey("Mode"))
                {
                    exceptionMsg.AppendLine("Mode is missing.");
                }
                if (!GetParameters.ContainsKey("PrintSetupFile"))
                {
                    exceptionMsg.AppendLine("PrintSetupFile is missing.");
                }
                throw new ArgumentException(exceptionMsg.ToString());
            }
            else
            {
                ProcessReport();
            }
        }

        static void ProcessReport()
        {
            


            var reportPath = GetParameters["ReportPath"];
            var dsFile = GetParameters["ReportDataSource"];
            var ds = File.ReadAllText(dsFile).FL_CastXmlToDataSet();

            var reportName = GetParameters["ReportName"];
            var mode = GetParameters["Mode"];
            var printSetupFile = GetParameters["PrintSetupFile"];


            if (ds.Tables.Count == 0)
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

            

            var rldcReportCompiler = new FL_IRdlcReport()
            {
                DataTables = File.ReadAllText(dsFile).FL_CastXmlToDataSet(),
                ReportFile = reportPath,
                ReportName = reportName,
                PrintSettingFilePath = printSetupFile
            };

            /*
            var rldcReportCompiler = new FL_IRdlcReport()
            {
                DataTables = ds,
                ReportFile = reportPath,
                ReportName = reportName,
                PrintSettingFilePath = printSetupFile
            };
            */

            if (ds.Tables.Cast<DataTable>().Any(x => x.TableName == "RldcParameters"))
            {
                //load the parameters
                var ParametersCount = ds.Tables["RldcParameters"].Rows.Count;
                if (ParametersCount > 0)
                {
                    //load the parameters in rlcdReportCompiler
                    rldcReportCompiler.ReportParameters = new List<FL_RdlcReportParameter>();

                    for (int i = 0; i < ParametersCount; i++)
                    {
                        rldcReportCompiler.ReportParameters.Add(new FL_RdlcReportParameter()
                        {
                            Name = ds.Tables["RldcParameters"].Rows[i]["Name"].ToString(),
                            Value = ds.Tables["RldcParameters"].Rows[i]["Value"]
                        });
                    }
                }
            }

            
            //check file exists
            if (!Directory.Exists(Path.GetDirectoryName(printSetupFile)))
            {
                //create the directory
                Directory.CreateDirectory(Path.GetDirectoryName(printSetupFile));
            }
            else if (File.Exists(printSetupFile))
            {
                if (!string.IsNullOrEmpty(File.ReadAllText(printSetupFile)))
                {
                    var PageSettings = File.ReadAllText(printSetupFile).FL_CastToClass<CustomPrintDialog>();
                    if (PageSettings != null && PageSettings.CPageSettings != null)
                    {
                        rldcReportCompiler.PrintSettings = PageSettings;
                    }
                    else
                    {
                        throw new Exception("Invalid PrintSetupFile");
                    }
                }
            }
            


            if (mode == "Preview" || mode == "PrintSetup")
            {
                rldcReportCompiler.AltTriggerPrintSettings = mode == "PrintSetup";
                //new ReportViewerForm(rldcReportCompiler).Show();
                //rldcReportCompiler.Preview();
                PreviewReport(rldcReportCompiler);
            }
            else if (mode == "Print")
            {
                //rldcReportCompiler.PrintToPrinter();
                PrintReport(rldcReportCompiler);
            }
            else if (mode == "Export")
            {
                ExportReport(rldcReportCompiler);
            }
            else
            {
                throw new Exception("Invalid mode.");
            }
        }

        static void PreviewReport(FL_IRdlcReport report)
        {

            using var form = new FL_RdlcReportViewerForm(report); // Ensure you have a form named ReportViewerForm with a ReportViewer control.
            form.ShowDialog();
        }

        static ExportFormat GetExportFormat()
        {
            var getExportFormat = GetParameters["ExportFormat"];
            if (string.IsNullOrEmpty(getExportFormat))
            {
                throw new Exception("ExportFormat is missing.");
            }

            return getExportFormat switch
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
                _ => ExportFormat.PDF,
            };
        }

        static void ExportReport(FL_IRdlcReport report)
        {
            var getExportFormat = GetParameters["ExportFormat"];
            if (string.IsNullOrEmpty(getExportFormat))
            {
                throw new Exception("ExportFormat is missing.");
            }

            ExportFormat exportFormat = ExportFormat.PDF;

            using var LocalReport = report.GetLoadedReport();
            lock (LocalReport)
            {
                report.ExportFileName = GetParameters["ExportPath"];
                report.ExportFormat = GetExportFormat();
            }
        }

        static void PrintReport(FL_IRdlcReport report)
        {
            using var LocalReport = report.GetLoadedReport();
            lock (LocalReport)
            {
                LocalReport.PrintToPrinter(report.PrintSettings);
            }

        }

    }

}
