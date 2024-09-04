

using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
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



            if (reportCompiler.TriggerPrintSettings)
            {
                //reportViewer.PageSetupDialog();
                var pageSetting = reportViewer?.GetPageSettings();
                reportCompiler.PrintSettings = new CustomPrintDialog(reportViewer.PrinterSettings, pageSetting);

                reportCompiler.PrintSettingFilePath = reportViewer?.PrintSettingFilePath;

                PrinterSettings ps = new PrinterSettings();
                if (reportCompiler?.PrintSettings != null)
                {

                    using (var pd = reportCompiler?.PrintSettings?.GetPrintDialog())
                    {


                        pd.PrinterSettings.DefaultPageSettings.PaperSize = pageSetting?.PaperSize;
                        pd.PrinterSettings.DefaultPageSettings.Landscape = (pageSetting?.Landscape).GetValueOrDefault(false);
                        pd.PrinterSettings.DefaultPageSettings.Margins = pageSetting?.Margins;
                        pd.PrinterSettings.DefaultPageSettings.Color = (pageSetting?.Color).GetValueOrDefault(false);
                        pd.PrinterSettings.DefaultPageSettings.PaperSource = pageSetting?.PaperSource;
                        pd.PrinterSettings.DefaultPageSettings.PrinterResolution = pageSetting?.PrinterResolution;

                        ps = reportViewer.GetPrintDialog(pd);

                    }
                }

                var pdz = new PrintDialog();
                pdz.PrinterSettings = ps;


                var ddr = reportViewer.PageSetupDialog();


                if (ddr == DialogResult.OK)
                {
                    //CurrentReport.PageSettings = pageSetupDialog.PageSettings;
                    //PrinterSettings = pageSetupDialog.PrinterSettings;


                    reportViewer.PrinterSettings = ps;

                    //update the page settings and printersettings in reportcompiler
                    reportCompiler.PrintSettings = new CustomPrintDialog(pdz, reportViewer.CurrentReportPageSetting);
                    if (!string.IsNullOrEmpty(reportCompiler.PrintSettingFilePath))
                    {
                        //update the compiler print settings file
                        var json = reportCompiler.PrintSettings.FL_CastToJson();
                        File.WriteAllText(reportCompiler.PrintSettingFilePath, json);
                    }

                    reportViewer.RefreshReport();
                }

            }
            else
            {
                if (reportCompiler.PrintSettings != null)
                {
                    reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
                    reportViewer.SetPageSettings(reportCompiler.PrintSettings.CPageSettings.GetPageSettings());
                    reportViewer.Refresh();
                    if (reportCompiler.TriggerPrint)
                    {
                        reportCompiler.TriggerPrint = false;
                        reportViewer.DPrint();

                        this.Close();
                    }
                }
            }

            base.OnLoad(e);
        }

        public void Print()
        {
            /*
            //reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
            reportCompiler.Load(reportViewer.LocalReport);
            reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath; 
            if (File.Exists(reportCompiler.ReportFile))
            {
                try
                {
                    var customPrintDialog = File.ReadAllLines(reportCompiler.ReportFile).FL_CastToClass<CustomPrintDialog>();

                    reportViewer.CustomPrintDialog = customPrintDialog;
                }
                catch (Exception ex)
                {

                }
            }
            reportViewer.RefreshReport();
            reportViewer.m_lastUIState = UIState.ProcessingPartial;
            */

            Load();
            reportViewer.RefreshReport();
            base.OnLoad(null);
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

            reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
            if (File.Exists(reportCompiler.ReportFile))
            {
                try
                {
                    var customPrintDialog = File.ReadAllLines(reportCompiler.ReportFile).FL_CastToClass<CustomPrintDialog>();

                    reportViewer.CustomPrintDialog = customPrintDialog;
                }
                catch (Exception ex)
                {

                }
            }
            reportViewer.CustomPrintDialog = reportCompiler.PrintSettings;
            var rqdParameters = reportViewer.LocalReport.GetParameters().Count;



            if (rqdParameters > 0)
            {

                if (reportCompiler.ReportParameters == null || reportCompiler.ReportParameters.Count == 0)
                {
                    throw new Exception("No parameters to load");
                }

                var rqdParametersList = reportViewer.LocalReport.GetParameters().ToList();
                if (rqdParameters > reportCompiler.ReportParameters.Count)
                {
                    //find the missing parameters
                    var missingParameters = rqdParametersList.FindAll(x => reportCompiler.ReportParameters.Find(y => y.Name == x.Name) == null);
                    //throw exception mentioning the missing parameters
                    throw new Exception($"Missing parameters: {string.Join(",", missingParameters)}");
                }
                var _rqdParametersList = rqdParametersList.Select(x => x.Name).ToList();
                //add the parameters which are required by the report
                var rpp = new List<FL_RdlcReportParameter>();
                _rqdParametersList.ForEach(x =>
                {
                    var parameter = reportCompiler.ReportParameters.Where(y => y.Name == x);
                    if (parameter.Count() == 0)
                    {
                        throw new Exception($"Missing parameter: {x}");
                    }
                    rpp.Add(parameter.First());
                });

                //set the parameters
                reportViewer.LocalReport.SetParameters(rpp.Select(p => new ReportParameter(p.Name, p.Value.ToString())));
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
