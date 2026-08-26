using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.Design.Tests;

public sealed class RdlcDocumentTests
{
    [Fact]
    public void Existing_tablix_fixture_loads_as_a_typed_document()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TwoPageTablix.rdlc");
        var document = RdlcDocument.Load(File.ReadAllText(path));

        Assert.Equal("2016/01/reportdefinition", document.Report.NamespaceUri.Split("sqlserver/reporting/", StringSplitOptions.None)[1]);
        Assert.Equal("TwoPageTablix", Assert.Single(document.Report.Sections).Body!.ReportItems[0].Name);
        Assert.Equal(document.ToXml(), RdlcDocument.Load(document.ToXml()).ToXml());
    }

    [Fact]
    public void Load_exposes_core_report_sections_and_collections()
    {
        var document = RdlcDocument.Load(Xml("<DataSources><DataSource Name=\"Orders\"><ConnectionProperties><DataProvider>System.Data.DataSet</DataProvider></ConnectionProperties></DataSource></DataSources><DataSets><DataSet Name=\"OrdersSet\"><Query><DataSourceName>Orders</DataSourceName></Query></DataSet></DataSets><ReportSections><ReportSection><Body><ReportItems><Textbox Name=\"Title\" /></ReportItems></Body></ReportSection></ReportSections><ReportParameters><ReportParameter Name=\"Company\"><DataType>String</DataType></ReportParameter></ReportParameters>"));

        Assert.Single(document.Report.DataSources);
        Assert.Equal("OrdersSet", Assert.Single(document.Report.DataSets).Name);
        Assert.Equal("Title", Assert.Single(document.Report.Sections[0].Body!.ReportItems).Name);
        Assert.Equal("Company", Assert.Single(document.Report.Parameters).Name);
    }

    [Fact]
    public void Unknown_extensions_and_order_are_preserved()
    {
        const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Report xmlns=\"urn:test\" xmlns:x=\"urn:extension\"><CustomBefore /><ReportSections><ReportSection><Body><ReportItems><x:Widget z=\"1\" /></ReportItems><x:BodyExtension /></Body></ReportSection></ReportSections><CustomAfter /></Report>";
        var document = RdlcDocument.Load(xml);
        var saved = document.ToXml();

        Assert.Contains("<CustomBefore", saved);
        Assert.Contains("<x:Widget z=\"1\"", saved);
        Assert.Contains("<x:BodyExtension", saved);
        Assert.True(saved.IndexOf("CustomBefore", StringComparison.Ordinal) < saved.IndexOf("ReportSections", StringComparison.Ordinal));
        Assert.True(saved.IndexOf("ReportSections", StringComparison.Ordinal) < saved.IndexOf("CustomAfter", StringComparison.Ordinal));
    }

    [Fact]
    public void Malformed_xml_has_a_clear_diagnostic()
    {
        var error = Assert.Throws<RdlcDocumentFormatException>(() => RdlcDocument.Load("<Report>"));
        Assert.Contains("malformed XML", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_save_load_is_deterministic()
    {
        var first = RdlcDocument.Load(Xml("<AutoRefresh>0</AutoRefresh><ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var second = RdlcDocument.Load(first.ToXml());

        Assert.Equal(first.ToXml(), second.ToXml());
    }

    [Fact]
    public void Missing_report_root_is_reported_as_unsupported_required_structure()
    {
        var error = Assert.Throws<RdlcDocumentFormatException>(() => RdlcDocument.Load("<NotReport />"));
        Assert.Contains("Report root", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_report_sections_is_reported_as_unsupported_required_structure()
    {
        var error = Assert.Throws<RdlcDocumentFormatException>(() => RdlcDocument.Load(Xml("<DataSets />")));
        Assert.Contains("ReportSections", error.Message, StringComparison.Ordinal);
    }

    private static string Xml(string content) => $"<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition\">{content}</Report>";
}
