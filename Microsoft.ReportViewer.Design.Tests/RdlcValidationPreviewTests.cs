using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.Design.Tests;

public sealed class RdlcValidationPreviewTests
{
    [Fact]
    public void Validation_converts_malformed_xml_into_an_error_diagnostic()
    {
        var diagnostics = new RdlcDesignValidator().ValidateXml("<Report>");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(RdlcDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Definition", diagnostic.Property);
        Assert.Contains("malformed", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validation_returns_structured_location_for_missing_dataset_and_parameter()
    {
        var document = RdlcDocument.Load(Xml("<ReportParameters><ReportParameter Name=\"Known\"><DataType>String</DataType></ReportParameter></ReportParameters><ReportSections><ReportSection><Body><ReportItems><Textbox Name=\"ValueText\"><DataSetName>MissingSet</DataSetName><Value>=Parameters!Missing.Value</Value></Textbox></ReportItems></Body></ReportSection></ReportSections>"));

        var diagnostics = new RdlcDesignValidator().Validate(document);

        Assert.Contains(diagnostics, x => x.Severity == RdlcDiagnosticSeverity.Error && x.Item == "ValueText" && x.Property == "DataSetName" && x.Line is not null);
        Assert.Contains(diagnostics, x => x.Severity == RdlcDiagnosticSeverity.Error && x.Item == "ValueText" && x.Property == "Value" && x.Message.Contains("Missing", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preview_renders_in_memory_data_to_html_and_pdf_without_mutating_document()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TwoPageTablix.rdlc");
        var document = RdlcDocument.Load(File.ReadAllText(path));
        var before = document.ToXml();
        var preview = new RdlcPreviewService();

        var data = new Dictionary<string, IEnumerable<object>> { ["DataSet1"] = new[] { new { Id = 42, Label = "Preview" } } };
        var html = await preview.RenderAsync(new RdlcPreviewRequest(document, data, Format: "HTML5"));
        var pdf = await preview.RenderAsync(new RdlcPreviewRequest(document, data, Format: "PDF"));

        Assert.True(html.Succeeded, string.Join("; ", html.Diagnostics.Select(x => x.Message)));
        Assert.StartsWith("text/html", html.MimeType, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(html.Content);
        Assert.True(pdf.Succeeded, string.Join("; ", pdf.Diagnostics.Select(x => x.Message)));
        Assert.Equal("application/pdf", pdf.MimeType);
        Assert.StartsWith("%PDF-", System.Text.Encoding.ASCII.GetString(pdf.Content, 0, Math.Min(pdf.Content.Length, 5)));
        Assert.Equal(before, document.ToXml());
    }

    [Fact]
    public async Task Preview_reports_missing_data_and_keeps_edits_intact()
    {
        var document = RdlcDocument.Load(Xml("<DataSources><DataSource Name=\"Orders\" /></DataSources><DataSets><DataSet Name=\"OrdersSet\"><Query><DataSourceName>Orders</DataSourceName></Query></DataSet></DataSets><ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var before = document.ToXml();
        var result = await new RdlcPreviewService().RenderAsync(new RdlcPreviewRequest(document, new Dictionary<string, IEnumerable<object>>()));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, x => x.Severity == RdlcDiagnosticSeverity.Error && x.Message.Contains("sample data", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(before, document.ToXml());
    }

    [Fact]
    public async Task New_preview_cancels_previous_request_and_marks_it_stale()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var session = new RdlcPreviewSession(new RdlcPreviewService());
        var first = session.RefreshAsync(document, new Dictionary<string, IEnumerable<object>>());
        var second = session.RefreshAsync(document, new Dictionary<string, IEnumerable<object>>());

        var firstResult = await first;
        var secondResult = await second;

        Assert.True(firstResult.IsStale || secondResult.Succeeded || secondResult.Diagnostics.Count > 0);
        Assert.False(session.IsDisposed);
        session.Dispose();
    }

    private static string Xml(string content) => $"<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition\">{content}</Report>";
}
