using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using System;
using System.Data;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CliReportCompiler.ReportForm
{
    public class FL_RdlcReportViewerForm : Form
    {
        private readonly ReportViewer reportViewer;
        private bool reportLoaded;

        public FL_IRdlcReport reportCompiler { get; set; } = new();

        public FL_RdlcReportViewerForm()
        {
            Text = "Report viewer";
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(reportViewer);
        }

        public FL_RdlcReportViewerForm(FL_IRdlcReport reportCompiler)
            : this()
        {
            this.reportCompiler = reportCompiler ?? throw new ArgumentNullException(nameof(reportCompiler));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadReport();
            reportViewer.RefreshReport();

            if (reportCompiler.TriggerPrintSettings)
            {
                ShowPrintSetup();
            }
            else
            {
                ApplyStoredPrintSettings();
            }
        }

        public void Print()
        {
            LoadReport();
            reportViewer.RefreshReport();
            reportViewer.DPrint();
        }

        private void LoadReport()
        {
            if (reportLoaded)
            {
                return;
            }

            if (reportCompiler.DataTables == null || reportCompiler.DataTables.Tables.Count == 0)
            {
                throw new InvalidOperationException("No data to load");
            }

            if (string.IsNullOrWhiteSpace(reportCompiler.ReportFile))
            {
                throw new InvalidOperationException("No report file to load");
            }

            if (!File.Exists(reportCompiler.ReportFile))
            {
                throw new FileNotFoundException("Report file not found", reportCompiler.ReportFile);
            }

            using (var reportStream = File.OpenRead(reportCompiler.ReportFile))
            {
                reportViewer.LocalReport.LoadReportDefinition(reportStream);
            }

            var hasSubReports = LoadSubreports();
            LoadParameters();
            BindDataSources();

            if (hasSubReports)
            {
                reportViewer.LocalReport.SubreportProcessing += (_, eventArgs) =>
                {
                    foreach (DataTable table in reportCompiler.DataTables.Tables)
                    {
                        eventArgs.DataSources.Add(new ReportDataSource(table.TableName, table));
                    }
                };
            }

            reportViewer.PrintSettingFilePath = reportCompiler.PrintSettingFilePath;
            reportViewer.CustomPrintDialog = reportCompiler.PrintSettings;
            reportLoaded = true;
        }

        private bool LoadSubreports()
        {
            var subReports = reportCompiler.SubReports;
            if (subReports == null || subReports.Count == 0)
            {
                return false;
            }

            reportViewer.LocalReport.ShowDetailedSubreportMessages = true;
            var loadedSubreport = false;

            foreach (var subReport in subReports)
            {
                if (string.IsNullOrWhiteSpace(subReport.Value) || !File.Exists(subReport.Value))
                {
                    continue;
                }

                using var subReportStream = File.OpenRead(subReport.Value);
                reportViewer.LocalReport.LoadSubreportDefinition(subReport.Key, subReportStream);
                loadedSubreport = true;
            }

            return loadedSubreport;
        }

        private void LoadParameters()
        {
            var requiredParameters = reportViewer.LocalReport.GetParameters().ToList();
            if (requiredParameters.Count == 0)
            {
                return;
            }

            if (reportCompiler.ReportParameters == null || reportCompiler.ReportParameters.Count == 0)
            {
                throw new InvalidDataException("No parameters to load");
            }

            var suppliedParameters = reportCompiler.ReportParameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Name))
                .GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

            var missingParameters = requiredParameters
                .Where(parameter => !suppliedParameters.ContainsKey(parameter.Name))
                .Select(parameter => parameter.Name)
                .ToArray();

            if (missingParameters.Length > 0)
            {
                throw new InvalidDataException($"Missing parameters: {string.Join(",", missingParameters)}");
            }

            reportViewer.LocalReport.SetParameters(requiredParameters.Select(parameter =>
            {
                var suppliedParameter = suppliedParameters[parameter.Name];
                return new ReportParameter(parameter.Name, suppliedParameter.Value?.ToString() ?? string.Empty);
            }));
        }

        private void BindDataSources()
        {
            foreach (DataTable table in reportCompiler.DataTables.Tables)
            {
                reportViewer.LocalReport.DataSources.Add(new ReportDataSource(table.TableName, table));
            }

            reportViewer.LocalReport.EnableExternalImages = true;
            reportViewer.LocalReport.EnableHyperlinks = true;
        }

        private void ApplyStoredPrintSettings()
        {
            if (reportCompiler.PrintSettings?.CPageSettings == null)
            {
                return;
            }

            reportViewer.SetPageSettings(reportCompiler.PrintSettings.CPageSettings.GetPageSettings());
            reportViewer.Refresh();

            if (reportCompiler.TriggerPrint)
            {
                reportCompiler.TriggerPrint = false;
                reportViewer.DPrint();
                Close();
            }
        }

        private void ShowPrintSetup()
        {
            var pageSetting = reportViewer.GetPageSettings();
            reportCompiler.PrintSettings = new CustomPrintDialog(reportViewer.PrinterSettings, pageSetting);
            reportViewer.CustomPrintDialog = reportCompiler.PrintSettings;

            PrinterSettings printerSettings;
            using (var printDialog = reportCompiler.PrintSettings.GetPrintDialogSettings().CreatePrintDialog())
            {
                ApplyPageSettings(printDialog, pageSetting);
                printerSettings = reportViewer.GetPrintDialog(printDialog);
            }

            using var configuredDialog = new PrintDialog
            {
                PrinterSettings = printerSettings
            };

            if (reportViewer.PageSetupDialog() != DialogResult.OK)
            {
                return;
            }

            reportViewer.PrinterSettings = printerSettings;
            reportCompiler.PrintSettings = new CustomPrintDialog(configuredDialog, reportViewer.CurrentReportPageSetting);
            reportViewer.CustomPrintDialog = reportCompiler.PrintSettings;

            if (!string.IsNullOrWhiteSpace(reportCompiler.PrintSettingFilePath))
            {
                File.WriteAllText(reportCompiler.PrintSettingFilePath, reportCompiler.PrintSettings.FL_CastToJson());
            }

            reportViewer.RefreshReport();
        }

        private static void ApplyPageSettings(PrintDialog printDialog, PageSettings pageSetting)
        {
            var defaultPageSettings = printDialog.PrinterSettings.DefaultPageSettings;
            defaultPageSettings.PaperSize = pageSetting.PaperSize;
            defaultPageSettings.Landscape = pageSetting.Landscape;
            defaultPageSettings.Margins = pageSetting.Margins;
            defaultPageSettings.Color = pageSetting.Color;
            defaultPageSettings.PaperSource = pageSetting.PaperSource;
            defaultPageSettings.PrinterResolution = pageSetting.PrinterResolution;
        }
    }
}
