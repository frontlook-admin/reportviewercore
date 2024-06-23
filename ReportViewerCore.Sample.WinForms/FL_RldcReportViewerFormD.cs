
using FrontLookCoreDbAccessLibrary.FL_FastReport;
using Microsoft.Reporting.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ReportViewerCore
{
    public class FL_RldcReportViewerFormD : Form
    {
        private readonly ReportViewer reportViewer;
        public FL_IRdlcReportD reportCompiler { get; set; } = new FL_IRdlcReportD();

        public FL_RldcReportViewerFormD(FL_IRdlcReportD _reportCompiler)
        {
            Text = "Report viewer";
            reportCompiler = _reportCompiler;
            WindowState = FormWindowState.Maximized;
            reportViewer = new ReportViewer();
            reportViewer.Dock = DockStyle.Fill;
            Controls.Add(reportViewer);
        }

        public FL_RldcReportViewerFormD()
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


    public class FL_IRdlcReportD
    {

        public DataSet DataTables { get; set; }
        public List<FL_ReportParameter> ReportParameters { get; set; } = new();
        public string ReportFile { get; set; } = "";
        public string ReportName { get; set; } = "";


        public void Preview()
        {
            using var form = new FL_RldcReportViewerFormD(this);
            form.ShowDialog();
        }

        public void Load(LocalReport report)
        {
            if (DataTables == null || DataTables.Tables.Count == 0)
            {
                throw new Exception("No data to load");
            }
            if (string.IsNullOrEmpty(ReportFile))
            {
                throw new Exception("No report file to load");
            }
            if (!File.Exists(ReportFile))
            {
                throw new Exception("Report file not found");
            }
            using var fs = new FileStream(ReportFile, FileMode.Open);
            report.LoadReportDefinition(fs);
            var rqdParameters = report.GetParameters().Count;



            if (rqdParameters > 0)
            {

                if ((ReportParameters == null || ReportParameters.Count == 0))
                {
                    throw new Exception("No parameters to load");
                }
                if (rqdParameters > ReportParameters.Count)
                {
                    //find the missing parameters
                    var missingParameters = report.GetParameters().ToList().FindAll(x => ReportParameters.Find(y => y.Name == x.Name) == null);
                    //throw exception mentioning the missing parameters
                    throw new Exception($"Missing parameters: {string.Join(",", missingParameters)}");
                }

                //set the parameters
                report.SetParameters(ReportParameters.Select(p => new ReportParameter(p.Name, p.Value.ToString())));
            }

            DataTables.Tables.Cast<DataTable>().ToList().ForEach(x =>
            {
                report.DataSources.Add(new ReportDataSource(x.TableName, x));
            });

        }

        public LocalReport GetLoadedReport()
        {
            var report = new LocalReport();
            if (DataTables == null || DataTables.Tables.Count == 0)
            {
                throw new Exception("No data to load");
            }
            if (string.IsNullOrEmpty(ReportFile))
            {
                throw new Exception("No report file to load");
            }
            if (!File.Exists(ReportFile))
            {
                throw new Exception("Report file not found");
            }
            using var fs = new FileStream(ReportFile, FileMode.Open);
            report.LoadReportDefinition(fs);
            var rqdParameters = report.GetParameters().Count;



            if (rqdParameters > 0)
            {

                if ((ReportParameters == null || ReportParameters.Count == 0))
                {
                    throw new Exception("No parameters to load");
                }
                if (rqdParameters > ReportParameters.Count)
                {
                    //find the missing parameters
                    var missingParameters = report.GetParameters().ToList().FindAll(x => ReportParameters.Find(y => y.Name == x.Name) == null);
                    //throw exception mentioning the missing parameters
                    throw new Exception($"Missing parameters: {string.Join(",", missingParameters)}");
                }

                DataTables.Tables.Cast<DataTable>().ToList().ForEach(x =>
                {
                    report.DataSources.Add(new ReportDataSource(x.TableName, x));
                });
            }

            DataTables.Tables.Cast<DataTable>().ToList().ForEach(x =>
            {
                report.DataSources.Add(new ReportDataSource(x.TableName, x));
            });

            return report;
        }

        public void LoadReport(LocalReport report)
        {
            Load(report);
        }

        //get pdf bytes
        public byte[] GetPdfBytes(LocalReport report = null)
        {
            if (report == null)
            {
                report = GetLoadedReport();
            }
            else
            {
                Load(report);
            }
            return report.Render("PDF");
        }

        //get xlsx bytes
        public byte[] GetXlsxBytes(LocalReport report = null)
        {
            if (report == null)
            {
                report = GetLoadedReport();
            }
            else
            {
                Load(report);
            }
            return report.Render("Excel");
        }

        //get word bytes
        public byte[] GetWordBytes(LocalReport report = null)
        {
            if (report == null)
            {
                report = GetLoadedReport();
            }
            else
            {
                Load(report);
            }
            return report.Render("Word");
        }

        //get image bytes
        public byte[] GetImageBytes(LocalReport report = null)
        {
            if (report == null)
            {
                report = GetLoadedReport();
            }
            else
            {
                Load(report);
            }
            return report.Render("Image");
        }
    }
}
