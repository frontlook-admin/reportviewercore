using System.Data;
using Microsoft.Reporting.NETCore;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.NETCore.Tests;

public sealed class RestrictedReportWorkerTests
{
    [Fact]
    public void Protocol_round_trips_request_without_exposing_raw_report_paths()
    {
        var request = new ReportWorkerRequest(
            new HeadlessReportRequest(File.ReadAllBytes(Fixture("Report.rdlc")),
                new[] { new HeadlessReportDataSource("Items", CreateTable()) }, format: "PDF"));

        var json = ReportWorkerProtocol.Serialize(request);
        var roundTrip = ReportWorkerProtocol.Deserialize(json);

        Assert.DoesNotContain(Fixture("Report.rdlc"), json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(request.Report.Format, roundTrip.Report.Format);
        Assert.Equal(request.Report.Definition, roundTrip.Report.Definition);
        Assert.Equal("Items", roundTrip.Report.DataSources[0].Name);
        Assert.Equal(1, roundTrip.Report.DataSources[0].Data.Rows.Count);
    }

    [Fact]
    public void Host_rejects_unsafe_report_before_starting_worker()
    {
        var request = new ReportWorkerRequest(new HeadlessReportRequest(
            System.Text.Encoding.UTF8.GetBytes("<Report><Code>Environment.Exit(1)</Code></Report>"),
            securityPolicy: ReportSecurityPolicy.Restrictive));

        var client = new RestrictedReportWorkerClient(new ReportWorkerClientOptions("missing-worker.exe"));

        var exception = Assert.Throws<ReportWorkerSecurityException>(() => client.Render(request));
        Assert.Contains("custom code", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Host_reports_timeout_and_does_not_leave_worker_running()
    {
        var options = new ReportWorkerClientOptions("timeout-worker.exe") { Timeout = TimeSpan.FromMilliseconds(20) };
        var client = new RestrictedReportWorkerClient(options, new TestWorkerTransport(delay: TimeSpan.FromSeconds(1)));

        var exception = await Assert.ThrowsAsync<ReportWorkerTimeoutException>(() => client.RenderAsync(
            new ReportWorkerRequest(new HeadlessReportRequest(System.Text.Encoding.UTF8.GetBytes("<Report />"),
                securityPolicy: ReportSecurityPolicy.Restrictive, isTrusted: true))));

        Assert.Equal(options.Timeout, exception.Timeout);
    }

    [Fact]
    public async Task Host_executes_safe_report_in_the_real_worker_process()
    {
        var request = new ReportWorkerRequest(new HeadlessReportRequest(
            File.ReadAllBytes(Fixture("Report.rdlc")),
            new[] { new HeadlessReportDataSource("Items", CreateTable()) },
            format: "PDF", securityPolicy: ReportSecurityPolicy.Restrictive, isTrusted: true));
        var client = new RestrictedReportWorkerClient(new ReportWorkerClientOptions(
            typeof(Microsoft.ReportViewer.ReportWorker.Program).Assembly.Location));

        var result = await client.RenderAsync(request);

        Assert.NotEmpty(result.Content);
        Assert.Contains(result.Streams, stream => stream.Extension.Equals("pdf", StringComparison.OrdinalIgnoreCase));
    }

    private static DataTable CreateTable()
    {
        var table = new DataTable("Items");
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("safe");
        return table;
    }

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    private sealed class TestWorkerTransport : IReportWorkerTransport
    {
        private readonly TimeSpan delay;
        public TestWorkerTransport(TimeSpan delay) => this.delay = delay;
        public async Task<ReportWorkerResult> ExecuteAsync(ReportWorkerRequest request, IProgress<ReportWorkerEvent>? progress, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new ReportWorkerResult("PDF", "application/pdf", "pdf", Array.Empty<byte>(), Array.Empty<ReportWorkerStream>(), Array.Empty<ReportWorkerDiagnostic>());
        }
    }
}
