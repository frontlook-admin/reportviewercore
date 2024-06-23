using FrontLookCoreLibraryAssembly.FL_General;
using Microsoft.Reporting.WinForms;
using ReportViewerCore.Sample.WinForms;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace ReportViewerCore
{
	class Report
	{
		public static void Load(LocalReport report)
		{
			var items = new[] { new ReportItem { Description = "Widget 6000", Price = 104.99m, Qty = 1 }, new ReportItem { Description = "Gizmo MAX", Price = 1.41m, Qty = 25 } };
			//var jsonFile = File.ReadAllText(@"SaleBill_Thermal1.json");
			//var dt = jsonFile.FL_CastToClass<List<RdlcData>>();
			//dt.ForEach(x => report.DataSources.Add(new ReportDataSource(x.DataName, x.DataValue)));


			var parameters = new[] { new ReportParameter("Title", "Invoice 4/2020") };
			using var fs = new FileStream("Report.rdlc", FileMode.Open);
			report.LoadReportDefinition(fs);
			report.DataSources.Add(new ReportDataSource("Items", items));
			report.SetParameters(parameters);
		}

		public static void Loadx(LocalReport report)
		{
			var items = new[] { new ReportItem { Description = "Widget 6000", Price = 104.99m, Qty = 1 }, new ReportItem { Description = "Gizmo MAX", Price = 1.41m, Qty = 25 } };
			var jsonFile = File.ReadAllText(@"Trial_Balance_Report.json");
			var dt = jsonFile.FL_CastToClass<List<RdlcDatal>>();

			//var parameters = new[] { new ReportParameter("Title", "Invoice 4/2020") };
			using var fs = new FileStream("TrialBalanceReport.rdlc", FileMode.Open);
			report.LoadReportDefinition(fs);

			//report.DataSources.Add(new ReportDataSource("Items", items));



			var dataTables = new DataSet();


			dt.ForEach(x =>
			{
				if(x.DataName == "CompanyInfos")
				{

					dataTables.Tables.Add((x.DataValue.FL_CastToClass<CompanyInfoVReport>()).FL_ConvertToDataTable(x.DataName));

					report.DataSources.Add(new ReportDataSource(x.DataName, (x.DataValue.FL_CastToClass<CompanyInfoVReport>()).FL_ConvertToDataTable(x.DataName)));

                }
				if(x.DataName == "MGeneralLedgers")
				{
					dataTables.Tables.Add((x.DataValue.FL_CastToClass<MGeneralLedgerVReport>()).FL_ConvertToDataTable(x.DataName));

                    report.DataSources.Add(new ReportDataSource(x.DataName, (x.DataValue.FL_CastToClass<MGeneralLedgerVReport>()).FL_ConvertToDataTable(x.DataName)));
                }

            });

			var reportCompiler = new FL_IRdlcReportD();
			reportCompiler.ReportFile = "TrialBalanceReport.rdlc";
			reportCompiler.DataTables = dataTables;
			reportCompiler.ReportParameters = new();
			reportCompiler.ReportName = "Trial Balance Report";


			var reportView = new FL_RldcReportViewerFormD(reportCompiler);
			reportView.ShowDialog();

			//report.DataSources.Add(new ReportDataSource("Items", items));
			//report.SetParameters(parameters);
		}

		public static void Loady(LocalReport report)
		{
			var jsonFile = File.ReadAllText(@"Trial_Balance_Report.json");
			var dt = jsonFile.FL_CastToClass<List<RdlcDatal>>();

			var dataTables = new DataSet();


			dt.ForEach(x =>
			{
				if(x.DataName == "CompanyInfos")
				{

					dataTables.Tables.Add((x.DataValue.FL_CastToClass<CompanyInfoVReport>()).FL_ConvertToDataTable(x.DataName));


                }
				if(x.DataName == "MGeneralLedgers")
				{
					dataTables.Tables.Add((x.DataValue.FL_CastToClass<MGeneralLedgerVReport>()).FL_ConvertToDataTable(x.DataName));

                }

            });

			var reportCompiler = new FL_IRdlcReportD();
			reportCompiler.ReportFile = "TrialBalanceReport.rdlc";
			reportCompiler.DataTables = dataTables;
			reportCompiler.ReportParameters = new();
			reportCompiler.ReportName = "Trial Balance Report";


			reportCompiler.Preview();

			//report.DataSources.Add(new ReportDataSource("Items", items));
			//report.SetParameters(parameters);
		}
	}
}
