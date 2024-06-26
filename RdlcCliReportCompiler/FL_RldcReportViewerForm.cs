
using FrontLookCoreDbAccessLibrary.Desktop.FL_RDLC;
using FrontLookCoreDbAccessLibrary.FL_FastReport;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CliReportCompiler
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


}
