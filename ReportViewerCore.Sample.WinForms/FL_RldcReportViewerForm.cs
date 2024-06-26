
using FrontLookCoreDbAccessLibrary.Desktop.FL_RDLC;
using FrontLookCoreDbAccessLibrary.FL_FastReport;
using Microsoft.Reporting.WinForms;
using NPOI.OpenXmlFormats.Vml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ReportViewerCore
{
    public class FL_RldcReportViewerForm : Form
    {
        private readonly ReportViewer reportViewer;
        public FL_IRdlcReport reportCompiler { get; set; } = new FL_IRdlcReport();

        public FL_RldcReportViewerForm(FL_IRdlcReport _reportCompiler)
        {
            Text = "Report viewer";
            reportCompiler = _reportCompiler;
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer();
            reportViewer.Dock = DockStyle.Fill;
            Controls.Add(reportViewer);
        }

        public FL_RldcReportViewerForm()
        {
            Text = "Report viewer";
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer();
            reportViewer.Dock = DockStyle.Fill;
            Controls.Add(reportViewer);
        }

        protected override void OnLoad(EventArgs e)
        {
            reportCompiler.Load(reportViewer.LocalReport);
            reportViewer.RefreshReport();
            base.OnLoad(e);



        }
    }
    /*
             @"Usage: SReportViewer.exe options
Options:
  --ReportPath|-rp      Path to the RDLC report file.
  --ReportDataSource|-ds Path to the data source file (XML/JSON).
  --Parameters|-p       JSON string of report parameters. Instead of using this option, you can also pass the parameters in the data source file with table name                            RldcParameters.
  --ReportName|-rn      Name of the report.
  --Mode|-m             Operation mode: Preview, Print, or Export.
  --ExportFormat|-ef    Export format: PDF, Excel, Word, or Image.
  --ExportPath|-ep      Path where the exported file will be saved.
  --Test|-t             Test message (for debugging purposes).
  --Demo|-d                Run a demo of the ReportViewer.
  --Help|-h             Display this help message.

Example:
  SReportViewer.exe --reportPath ""C:\path\to\report.rdlc"" --reportDataSource ""C:\path\to\data.xml"" --Parameters ""json Parameters"" --ReportName ""ReportName"" --Mode ""Preview"" --ExportFormat ""PDF"" --ExportPath ""C:\path\to\exported\file"" --test ""Msg""
  SReportViewer --help"
            */

}
