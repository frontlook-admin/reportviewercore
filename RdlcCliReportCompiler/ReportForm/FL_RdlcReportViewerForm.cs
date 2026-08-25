using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
using FrontLookCoreLibraryAssembly.FL_General;
using FrontLookCoreLibraryAssembly.FL_GlobalClasses;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.WinForms.FrontLookCode;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CliReportCompiler.ReportForm
{
    public class FL_RdlcReportViewerForm : Form
    {
        private readonly ReportViewer reportViewer;
        private readonly string[] liveReloadDataFilePaths = Array.Empty<string>();
        private bool reportLoaded;

        public FL_IRdlcReport reportCompiler { get; set; } = new();

        public FL_RdlcReportViewerForm()
        {
            Text = "Report Viewer";
            Icon = ReportViewerBranding.CreateApplicationIcon();
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

        public FL_RdlcReportViewerForm(
            FL_IRdlcReport reportCompiler,
            params string[] dataFilePaths)
            : this(reportCompiler)
        {
            liveReloadDataFilePaths = dataFilePaths?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
                ?? Array.Empty<string>();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            LoadReport();
            ApplyStoredPrintSettings();

            if (reportCompiler.TriggerPrintSettings)
            {
                ShowPrintSetup();
            }

            // Start in Normal mode. PrintLayout is an explicit toolbar action;
            // loading persisted printer settings must not change the preview.
            reportViewer.RefreshReport();

            ConfigureLiveReload();

            if (reportCompiler.TriggerPrint)
            {
                reportCompiler.TriggerPrint = false;
                reportViewer.DPrint();
                Close();
            }
        }

        public void Print()
        {
            LoadReport();
            ApplyStoredPrintSettings();
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

            reportViewer.LocalReport.DisplayName = reportCompiler.ReportName;

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

        private void ConfigureLiveReload()
        {
            if (!reportLoaded || string.IsNullOrWhiteSpace(reportCompiler.ReportFile))
            {
                return;
            }

            reportViewer.LiveReloadError += OnLiveReloadError;
            reportViewer.LiveReload.ReportDefinitionPath = reportCompiler.ReportFile;
            reportViewer.LiveReload.DataFilePaths = liveReloadDataFilePaths;
            reportViewer.LiveReload.ReloadAsync = ReloadReportAsync;
            reportViewer.LiveReload.Enabled = true;
        }

        private void OnLiveReloadError(object sender, ReportViewerLiveReloadErrorEventArgs e)
        {
            Debug.WriteLine($"RDLC live reload failed: {e.Exception.GetType().Name} - {e.Exception.Message}");
        }

        private async ValueTask<ReportViewerReloadSnapshot> ReloadReportAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reportDefinition = await File.ReadAllBytesAsync(
                reportCompiler.ReportFile,
                cancellationToken);
            var dataSet = await LoadReloadDataSetAsync(cancellationToken);
            var dataSources = dataSet.Tables
                .Cast<DataTable>()
                .Select(table => new ReportDataSource(table.TableName, table))
                .ToList();
            var parameters = GetReportParameters(reportDefinition, dataSet);

            return new ReportViewerReloadSnapshot(reportDefinition, dataSources, parameters);
        }

        private async ValueTask<DataSet> LoadReloadDataSetAsync(CancellationToken cancellationToken)
        {
            var dataFilePath = liveReloadDataFilePaths.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(dataFilePath))
            {
                return reportCompiler.DataTables
                    ?? throw new InvalidOperationException("No data to load");
            }

            var xml = await File.ReadAllTextAsync(dataFilePath, cancellationToken);
            var dataSet = xml.FL_CastXmlToDataSet();
            if (dataSet.Tables.Count == 0)
            {
                throw new InvalidDataException("No data tables found in the data source");
            }

            return dataSet;
        }

        private IReadOnlyList<ReportParameter> GetReportParameters(
            byte[] reportDefinition,
            DataSet dataSet)
        {
            using var report = new LocalReport();
            using var definition = new MemoryStream(reportDefinition, writable: false);
            report.LoadReportDefinition(definition);

            var suppliedParameters = GetSuppliedParameters(dataSet);
            var requiredParameters = report.GetParameters().ToList();
            if (requiredParameters.Count == 0)
            {
                return Array.Empty<ReportParameter>();
            }

            if (suppliedParameters.Count == 0)
            {
                throw new InvalidDataException("No parameters to load");
            }

            var missingParameters = requiredParameters
                .Where(parameter => !suppliedParameters.ContainsKey(parameter.Name))
                .Select(parameter => parameter.Name)
                .ToArray();
            if (missingParameters.Length > 0)
            {
                throw new InvalidDataException(
                    $"Missing parameters: {string.Join(",", missingParameters)}");
            }

            return requiredParameters
                .Select(parameter =>
                {
                    var suppliedParameter = suppliedParameters[parameter.Name];
                    return new ReportParameter(
                        parameter.Name,
                        suppliedParameter?.Value?.ToString() ?? string.Empty);
                })
                .ToList();
        }

        private Dictionary<string, FL_RdlcReportParameter> GetSuppliedParameters(DataSet dataSet)
        {
            var suppliedParameters = new Dictionary<string, FL_RdlcReportParameter>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var parameter in reportCompiler.ReportParameters ?? [])
            {
                if (!string.IsNullOrWhiteSpace(parameter.Name))
                {
                    suppliedParameters[parameter.Name] = parameter;
                }
            }

            if (dataSet.Tables.Contains("RldcParameters"))
            {
                var parameterTable = dataSet.Tables["RldcParameters"];
                if (parameterTable.Columns.Contains("Name") &&
                    parameterTable.Columns.Contains("Value"))
                {
                    foreach (DataRow row in parameterTable.Rows)
                    {
                        var name = row["Name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            suppliedParameters[name] = new FL_RdlcReportParameter
                            {
                                Name = name,
                                Value = row["Value"]
                            };
                        }
                    }
                }
            }

            return suppliedParameters;
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
            if (reportCompiler.PrintSettings == null)
            {
                return;
            }

            reportViewer.SetPageSettings(reportCompiler.PrintSettings.GetPageSettings());
        }

        private void ShowPrintSetup()
        {
            var pageSetting = reportViewer.GetPageSettings();
            if (reportCompiler.PrintSettings == null)
            {
                reportCompiler.PrintSettings = new CustomPrintDialog(reportViewer.PrinterSettings, pageSetting);
            }

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

            printerSettings = reportViewer.PrinterSettings;
            reportCompiler.PrintSettings = reportViewer.CustomPrintDialog ?? new CustomPrintDialog(configuredDialog, reportViewer.CurrentReportPageSetting);
            reportViewer.CustomPrintDialog = reportCompiler.PrintSettings;

            if (!string.IsNullOrWhiteSpace(reportCompiler.PrintSettingFilePath))
            {
                File.WriteAllText(reportCompiler.PrintSettingFilePath, reportCompiler.PrintSettings.FL_CastToJson());
            }
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
