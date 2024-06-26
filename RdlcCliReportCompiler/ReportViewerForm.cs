using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace CliReportCompiler
{
	public class ReportViewerForm : Form
	{
		private readonly ReportViewer reportViewer;

		public ReportViewerForm()
		{
			Text = "Report viewer";
			WindowState = FormWindowState.Maximized;
			reportViewer = new ReportViewer();
			reportViewer.Dock = DockStyle.Fill;
			Controls.Add(reportViewer);
		}

		protected override void OnLoad(EventArgs e)
		{
			Report.Loadx(reportViewer.LocalReport);
			reportViewer.RefreshReport();
			base.OnLoad(e);



		}
	}
}
