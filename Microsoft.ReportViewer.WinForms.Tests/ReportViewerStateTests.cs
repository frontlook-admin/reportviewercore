using System;
using FluentAssertions;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportViewerStateTests
{
    [Fact]
    public void State_is_immutable_and_exposes_render_contract()
    {
        var error = new InvalidOperationException("failed");
        var state = new ReportViewerState(
            currentPage: 2,
            totalPages: 7,
            pageCountMode: PageCountMode.Actual,
            zoomMode: ZoomMode.Percent,
            zoomPercent: 125,
            displayMode: DisplayMode.Normal,
            isLoading: true,
            error: error,
            isParameterDirty: true,
            renderGeneration: 9);

        state.CurrentPage.Should().Be(2);
        state.TotalPages.Should().Be(7);
        state.PageCountMode.Should().Be(PageCountMode.Actual);
        state.ZoomMode.Should().Be(ZoomMode.Percent);
        state.ZoomPercent.Should().Be(125);
        state.DisplayMode.Should().Be(DisplayMode.Normal);
        state.IsLoading.Should().BeTrue();
        state.Error.Should().BeSameAs(error);
        state.IsParameterDirty.Should().BeTrue();
        state.RenderGeneration.Should().Be(9);
        typeof(ReportViewerState).GetProperties().Should().OnlyContain(p => p.SetMethod == null);
    }

    [Fact]
    public void Viewer_exposes_state_snapshot_and_lifecycle_event()
    {
        var viewerType = typeof(Microsoft.Reporting.WinForms.ReportViewer);

        viewerType.GetProperty(nameof(Microsoft.Reporting.WinForms.ReportViewer.CurrentState))
            .Should().NotBeNull();
        viewerType.GetEvent(nameof(Microsoft.Reporting.WinForms.ReportViewer.StateChanged))
            .Should().NotBeNull();
    }

}
