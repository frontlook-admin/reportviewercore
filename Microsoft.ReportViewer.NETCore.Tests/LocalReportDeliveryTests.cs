using System.Reflection;
using Microsoft.Reporting.NETCore;
using Xunit;

namespace Microsoft.ReportViewer.NETCore.Tests;

public sealed class LocalReportDeliveryTests
{
    [Fact]
    public void Local_report_item_delivery_returns_actionable_local_processing_diagnostic()
    {
        using var report = new LocalReport();
        var method = typeof(LocalReport).GetMethod(
            "InternalDeliverReportItem",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var invocation = Assert.Throws<TargetInvocationException>(() =>
            method!.Invoke(report, new object?[]
            {
                "PDF",
                null,
                null,
                "description",
                "Bookmark",
                "match"
            }));

        var diagnostic = Assert.IsType<LocalProcessingException>(invocation.InnerException);
        Assert.Contains("PDF", diagnostic.Message);
        Assert.Contains("Bookmark", diagnostic.Message);
        Assert.Contains("ServerReport", diagnostic.Message);
    }
}
