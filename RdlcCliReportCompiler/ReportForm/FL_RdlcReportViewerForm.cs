

using FrontLookCoreDbAccessLibrary.Desktop.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.Common.FrontLookCode;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CliReportCompiler.ReportForm
{
    public class FL_RdlcReportViewerForm : Form
    {
        private readonly ReportViewer reportViewer;
        public FL_IRdlcReport reportCompiler { get; set; } = new FL_IRdlcReport();

        public FL_RdlcReportViewerForm(FL_IRdlcReport _reportCompiler)
        {
            Text = "Report viewer";
            reportCompiler = _reportCompiler;
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer();
            reportViewer.Dock = DockStyle.Fill;
            Controls.Add(reportViewer);
        }

        public FL_RdlcReportViewerForm()
        {
            Text = "Report viewer";
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer();
            reportViewer.Dock = DockStyle.Fill;
            Controls.Add(reportViewer);
        }

        protected override void OnLoad(EventArgs e)
        {
            Load();
            reportViewer.RefreshReport();
            base.OnLoad(e);

            

            if (reportCompiler.TriggerPrintSettings)
            {
                reportViewer.PageSetupDialog();

                var pageSetting = reportViewer?.GetPageSettings();
                reportCompiler.PrintSettings = new CustomPrintDialog(reportViewer.PrinterSettings, pageSetting);

                reportCompiler.PrintSettingFilePath = reportViewer?.PrintSettingFilePath;

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
            }
            else
            {
                if (reportCompiler.PrintSettings != null)
                {
                    reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
                    reportViewer.SetPageSettings(reportCompiler.PrintSettings.CPageSettings.GetPageSettings());
                    reportViewer.Refresh();
                }
                
            }

        }

        public void Print()
        {
            //reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
            reportCompiler.Load(reportViewer.LocalReport);
            reportViewer.RefreshReport();
            reportViewer.m_lastUIState = UIState.ProcessingPartial;
            reportViewer.DPrint();
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

                if (reportCompiler.ReportParameters == null || reportCompiler.ReportParameters.Count == 0)
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

            //add print settings file path
            if (!string.IsNullOrEmpty(reportCompiler.PrintSettingFilePath))
            {

                reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
            }
        }
    }


}
