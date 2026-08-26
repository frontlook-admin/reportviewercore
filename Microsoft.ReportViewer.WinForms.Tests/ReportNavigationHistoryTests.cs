using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportNavigationHistoryTests
{
    [Fact]
    public void Back_and_forward_restore_bookmark_and_drillthrough_context_without_duplicate_entries()
    {
        using var history = new ReportNavigationHistory();
        history.Record(new ReportNavigationEntry("main", 1, ReportNavigationKind.Page, null));
        history.Record(new ReportNavigationEntry("main", 3, ReportNavigationKind.Bookmark, "sales"));
        history.Record(new ReportNavigationEntry("detail", 1, ReportNavigationKind.Drillthrough, "orders"));

        history.CanGoBack.Should().BeTrue();
        history.TryGoBack(out var previous).Should().BeTrue();
        previous.ReportKey.Should().Be("main");
        previous.PageNumber.Should().Be(3);
        previous.Target.Should().Be("sales");
        history.TryGoForward(out var next).Should().BeTrue();
        next.ReportKey.Should().Be("detail");
        next.Kind.Should().Be(ReportNavigationKind.Drillthrough);
    }

    [Fact]
    public void Recording_after_back_discards_forward_entries_and_dispose_clears_state()
    {
        using var history = new ReportNavigationHistory();
        history.Record(new ReportNavigationEntry("main", 1, ReportNavigationKind.Page, null));
        history.Record(new ReportNavigationEntry("main", 2, ReportNavigationKind.Page, null));
        history.TryGoBack(out _).Should().BeTrue();
        history.Record(new ReportNavigationEntry("main", 4, ReportNavigationKind.DocumentMap, "chapter"));
        history.CanGoForward.Should().BeFalse();
        history.Dispose();
        history.CanGoBack.Should().BeFalse();
        history.TryGoBack(out _).Should().BeFalse();
    }
}
