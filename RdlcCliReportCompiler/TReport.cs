using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportingServices.ReportProcessing.ReportObjectModel;
using CliReportCompiler.Sample.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using DataSet = System.Data.DataSet;
using System.Linq;

namespace CliReportCompiler
{
	class TReport
	{
		public static void Load(LocalReport report)
		{
			var items = new[] { new ReportItem { Description = "Widget 6000", Price = 104.99m, Qty = 1 }, new ReportItem { Description = "Gizmo MAX", Price = 1.41m, Qty = 25 } };
			//var jsonFile = File.ReadAllText(@"SaleBill_Thermal1.json");
			//var dt = jsonFile.CastToClass<List<RdlcData>>();
			//dt.ForEach(x => report.DataSources.Add(new ReportDataSource(x.DataName, x.DataValue)));


			var parameters = new[] { new ReportParameter("Title", "Invoice 4/2020") };

			//make a temp copy of the report file and use it to load the report and delete it afterwards
			//this is necessary because the report file is locked by the report viewer control
			//and cannot be loaded directly from the original file
			var tempFile = Path.GetTempFileName();
			File.Copy("Report.rdlc", tempFile, true);

			using var fs = new FileStream(tempFile, FileMode.Open);
			report.LoadReportDefinition(fs);
			report.DataSources.Add(new ReportDataSource("Items", items));
			report.SetParameters(parameters);
		}

		public static void Loadx(LocalReport report)
		{
            var tempFile = Path.GetTempFileName();
            File.Copy("TrialBalanceReport.rdlc", tempFile, true);



            using var fs = new FileStream(tempFile, FileMode.Open);
            report.LoadReportDefinition(fs);


            var dataTables = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "DemoDataset.xml")).CastXmlToDataSet();



            dataTables.Tables.Cast<DataTable>().ToList().ForEach(x =>
			{

                report.DataSources.Add(new ReportDataSource(x.TableName, x));

            });
		}

	}
}
