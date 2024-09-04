

using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CliReportCompiler
{
    public class ReportViewerForm : Form
	{
		private readonly ReportViewer reportViewer;
        public static FL_IRdlcReport reportCompiler { get; set; }
        public bool Demo { get; set; }

        public ReportViewerForm(bool demo = false)
		{
			Text = "Report viewer";
			WindowState = FormWindowState.Maximized;
			reportViewer = new ReportViewer();
			reportViewer.Dock = DockStyle.Fill;
			Controls.Add(reportViewer);
            Demo = demo;
        }
        public ReportViewerForm(FL_IRdlcReport _reportCompiler)
        {
            Text = "Report viewer";
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer();
            reportViewer.Dock = DockStyle.Fill;
            Controls.Add(reportViewer);
            reportCompiler = _reportCompiler;
        }

        protected override void OnLoad(EventArgs e)
		{
            if (Demo)
            {

                TReport.Loadx(reportViewer.LocalReport);
                reportViewer.RefreshReport();
                base.OnLoad(e);
            }
            else
            {
                reportCompiler.Load(reportViewer.LocalReport);
                reportViewer.RefreshReport();
                base.OnLoad(e);



                if (reportCompiler.TriggerPrintSettings)
                {
                    reportViewer.SetPrinterAndPageSettings();
                    /*
                    reportViewer.PageSetupDialog();

                    var pageSetting = reportViewer?.GetPageSettings();
                    reportCompiler.PrintSettings = new CustomPrintDialog(reportViewer.PrinterSettings, pageSetting);
                    var pd = reportCompiler?.PrintSettings?.GetPrintDialog();

                    pd.PrinterSettings.DefaultPageSettings.PaperSize = pageSetting.PaperSize;
                    pd.PrinterSettings.DefaultPageSettings.Landscape = pageSetting.Landscape;
                    pd.PrinterSettings.DefaultPageSettings.Margins = pageSetting.Margins;
                    pd.PrinterSettings.DefaultPageSettings.Color = pageSetting.Color;
                    pd.PrinterSettings.DefaultPageSettings.PaperSource = pageSetting.PaperSource;
                    pd.PrinterSettings.DefaultPageSettings.PrinterResolution = pageSetting.PrinterResolution;
                    pd.ShowDialog();

                    reportViewer.PrinterSettings = pd.PrinterSettings;

                    //update the page settings and printersettings in reportcompiler
                    reportCompiler.PrintSettings = new CustomPrintDialog(pd, pageSetting);
                    if (!string.IsNullOrEmpty(reportCompiler.PrintSettingFilePath))
                    {
                        //update the compiler print settings file
                        var json = reportCompiler.PrintSettings.FL_CastToJson();
                        File.WriteAllText(reportCompiler.PrintSettingFilePath, json);
                    }

                    reportViewer.RefreshReport();
                    */
                }
                else
                {

                    if (reportCompiler.PrintSettings != null)
                    {
                        reportViewer.SetPageSettings(reportCompiler.PrintSettings.CPageSettings.GetPageSettings());
                        reportViewer.Refresh();
                    }

                }
            }
        }



        private void Load()
        {
            if (reportCompiler.DataTables == null || reportCompiler.DataTables.Tables.Count == 0)
            {
                throw new Exception("No data to load");
            }
            if (string.IsNullOrEmpty(reportCompiler.ReportFile))
            {
                throw new Exception("No report file to load");
            }
            if (!File.Exists(reportCompiler.ReportFile))
            {
                throw new Exception("Report file not found");
            }
            using var fs = new FileStream(reportCompiler.ReportFile, FileMode.Open);
            reportViewer.LocalReport.LoadReportDefinition(fs);
            var rqdParameters = reportViewer.LocalReport.GetParameters().Count;



            if (rqdParameters > 0)
            {

                if ((reportCompiler.ReportParameters == null || reportCompiler.ReportParameters.Count == 0))
                {
                    throw new Exception("No parameters to load");
                }
                if (rqdParameters > reportCompiler.ReportParameters.Count)
                {
                    //find the missing parameters
                    var missingParameters = reportViewer.LocalReport.GetParameters().ToList().FindAll(x => reportCompiler.ReportParameters.Find(y => y.Name == x.Name) == null);
                    //throw exception mentioning the missing parameters
                    throw new Exception($"Missing parameters: {string.Join(",", missingParameters)}");
                }

                //set the parameters
                reportViewer.LocalReport.SetParameters(reportCompiler.ReportParameters.Select(p => new ReportParameter(p.Name, p.Value.ToString())));
            }

            reportCompiler.DataTables.Tables.Cast<DataTable>().ToList().ForEach(x =>
            {
                reportViewer.LocalReport.DataSources.Add(new ReportDataSource(x.TableName, x));
            });

        }

        public void PrintSetUp()
        {
            reportViewer.SetPrinterAndPageSettings();
        }

        public void Print()
        {
            reportViewer.DPrint();
        }

        //get pdf bytes
        public byte[] GetExportBytes(ExportFormat format = ExportFormat.PDF, LocalReport report = null)
        {
            if (report == null)
            {
                report = reportViewer.LocalReport;
            }
            return report.Render(format.ToString());
        }



        //get pdf bytes
        public async Task<byte[]> GetExportBytesAsync(ExportFormat format = ExportFormat.PDF, LocalReport report = null)
        {
            if (report == null)
            {
                report = reportViewer.LocalReport;
            }
            return await report.RenderAsync(format.ToString());
        }

    }
}
