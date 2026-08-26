using System.Data;
using Microsoft.Reporting.NETCore;
using Xunit;

namespace Microsoft.ReportViewer.NETCore.Tests;

public sealed class HeadlessReportRendererTests
{
    [Fact]
    public void Security_contract_is_available_from_the_common_boundary()
    {
        var policy = Microsoft.Reporting.WinForms.ReportSecurityPolicy.Restrictive;
        var analysis = policy.Analyze("<Report><CodeModules><CodeModule>Unsafe.Module</CodeModule></CodeModules></Report>");

        Assert.Contains(analysis.Diagnostics, diagnostic =>
            diagnostic.Feature == Microsoft.Reporting.WinForms.ReportSecurityFeature.Assemblies &&
            diagnostic.Severity == Microsoft.Reporting.WinForms.ReportSecurityDiagnosticSeverity.Error);
    }

    [Fact]
    public void NetCore_local_report_exposes_the_shared_security_policy_and_diagnostics()
    {
        using var report = new LocalReport
        {
            SecurityPolicy = Microsoft.Reporting.WinForms.ReportSecurityPolicy.Restrictive
        };

        Assert.Throws<Microsoft.Reporting.WinForms.ReportSecurityException>(() =>
            report.LoadReportDefinition(new StringReader("<Report><Code>Unsafe()</Code></Report>")));
        Assert.Contains(report.SecurityDiagnostics, diagnostic =>
            diagnostic.Feature == Microsoft.Reporting.WinForms.ReportSecurityFeature.CustomCode);
    }

    [Theory]
    [InlineData("HTML4_0", "HTML4.0")]
    [InlineData("html4.0", "HTML4.0")]
    [InlineData("PDF", "PDF")]
    public void Renderer_UsesCanonicalFormatNames(string requested, string expected)
    {
        Assert.Equal(expected, HeadlessReportRenderer.GetRendererFormatName(requested));
    }

    [Fact]
    public void Request_ClonesMutableInputsAndExposesImmutableSnapshots()
    {
        var definition = new byte[] { 1, 2, 3 };
        var table = new DataTable("Items");
        table.Columns.Add("Name", typeof(string));
        var dataSources = new[] { new HeadlessReportDataSource("Items", table) };
        var parameters = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Title"] = new[] { "Before" }
        };

        var request = new HeadlessReportRequest(definition, dataSources, parameters, format: "CSV");
        definition[0] = 9;
        parameters["Title"] = new[] { "After" };

        Assert.Equal(new byte[] { 1, 2, 3 }, request.Definition);
        Assert.Equal("Before", request.Parameters["Title"][0]);
        Assert.Throws<NotSupportedException>(() => ((ICollection<HeadlessReportDataSource>)request.DataSources).Add(dataSources[0]));
    }

    [Fact]
    public async Task Renderer_ExportsParameterizedDatasetToCsvXmlPdfAndHtml()
    {
        var requestBase = CreateDemoRequest();
        foreach (var format in new[] { "CSV", "XML", "PDF", "HTML5" })
        {
            var result = await HeadlessReportRenderer.RenderAsync(requestBase with { Format = format });

            Assert.NotEmpty(result.Content);
            Assert.Equal(format, result.Format);
            Assert.NotEmpty(result.MimeType);
        }
    }

    [Fact]
    public async Task Renderer_LoadsSubreportDefinitionsAndReturnsAdditionalStreams()
    {
        var request = CreateDemoRequest() with
        {
            Format = "PDF",
            Subreports = new[]
            {
                new HeadlessSubreportDefinition("Detail", File.ReadAllBytes(Fixture("Report.rdlc")))
            }
        };

        var result = await HeadlessReportRenderer.RenderAsync(request);

        Assert.NotEmpty(result.Content);
        Assert.NotNull(result.Streams);
    }

    [Fact]
    public async Task Renderer_HonorsCancellationBeforeCreatingLocalReport()
    {
        var request = CreateDemoRequest() with { CancellationToken = new CancellationToken(canceled: true) };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => HeadlessReportRenderer.RenderAsync(request));
    }

    [Fact]
    public void Renderer_ValidatesParameterizedRequestWithoutRenderingOutput()
    {
        var request = CreateDemoRequest();

        HeadlessReportRenderer.Validate(request);
    }

    private static HeadlessReportRequest CreateDemoRequest()
    {
        var data = new DataTable("Items");
        data.Columns.Add("Description", typeof(string));
        data.Columns.Add("Price", typeof(decimal));
        data.Columns.Add("Qty", typeof(int));
        data.Columns.Add("Total", typeof(decimal));
        data.Rows.Add("Item", 12.5m, 2, 25m);

        return new HeadlessReportRequest(
            File.ReadAllBytes(Fixture("Report.rdlc")),
            new[] { new HeadlessReportDataSource("Items", data) },
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["Title"] = new[] { "Headless" }
            },
            format: "PDF");
    }

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
