using Microsoft.Reporting.WinForms;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.ReportViewer.Design.Tests;

public sealed class RdlcAdvancedAuthoringTests
{
    [Fact]
    public void Advanced_items_and_actions_round_trip_deterministically()
    {
        var document = RdlcDocument.Load(Xml("<DataSets><DataSet Name=\"Sales\" /></DataSets><ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);

        model.AddChart("SalesChart", "Sales", "0in", "0in", "5in", "3in");
        model.AddSubreport("Details", "SalesDetails", "0in", "3in", "5in", "2in");
        model.SetSubreportParameter("Details", "OrderId", "=Fields!OrderId.Value");
        model.SetBookmark("SalesChart", "=Fields!Region.Value");
        model.SetDrillthrough("SalesChart", "SalesDetails", new Dictionary<string, string> { ["OrderId"] = "=Fields!OrderId.Value" });
        model.SetInteractiveSort("SalesChart", "=Fields!Amount.Value", "SalesChart", "Descending");
        model.SetToggleItem("Details", "SalesChart");

        var xml = document.ToXml();
        Assert.Contains("<Chart ", xml);
        Assert.Contains("<Subreport ", xml);
        Assert.Contains("<ReportName>SalesDetails</ReportName>", xml);
        Assert.Contains("<Bookmark>=Fields!Region.Value</Bookmark>", xml);
        Assert.Contains("<Drillthrough>", xml);
        Assert.Contains("<UserSort>", xml);
        Assert.Contains("<ToggleItem>SalesChart</ToggleItem>", xml);
        Assert.Equal(xml, RdlcDocument.Load(xml).ToXml());
    }

    [Fact]
    public void Advanced_references_are_validated_and_extension_xml_is_preserved()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);
        var extension = XElement.Parse("<CustomMap xmlns=\"urn:vendor:map\"><Layer Name=\"Regions\" /></CustomMap>");

        model.AddExtensionReportItem("Map", "RegionalMap", extension, "0in", "0in", "4in", "3in");
        Assert.Throws<RdlcAuthoringException>(() => model.AddChart("MissingData", "Unknown", "0in", "0in", "1in", "1in"));
        Assert.Throws<RdlcAuthoringException>(() => model.AddExtensionReportItem("Unsupported", "Bad", extension, "0in", "0in", "1in", "1in"));

        var xml = document.ToXml();
        Assert.Contains("urn:vendor:map", xml);
        Assert.Contains("<Layer Name=\"Regions\"", xml);
        Assert.Equal(xml, RdlcDocument.Load(xml).ToXml());
    }

    [Fact]
    public void Interactive_sort_rejects_invalid_direction_without_mutating_xml()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Body><ReportItems><Textbox Name=\"Title\" /></ReportItems></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);
        var before = document.ToXml();

        Assert.Throws<RdlcAuthoringException>(() => model.SetInteractiveSort("Title", "=Fields!Name.Value", direction: "Random"));
        Assert.Equal(before, document.ToXml());
    }

    private static string Xml(string content) => $"<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition\">{content}</Report>";
}
