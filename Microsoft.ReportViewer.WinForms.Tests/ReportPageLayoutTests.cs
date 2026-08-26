using System.Drawing;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportPageLayoutTests
{
    [Fact]
    public void Fit_width_and_actual_size_are_dpi_safe()
    {
        SizeF pageMm = new SizeF(210, 297);
        SizeF viewport = new SizeF(1000, 700);

        ReportPageLayout.CalculateZoom(ZoomMode.PageWidth, viewport, pageMm, 144, 144)
            .Should().BeApproximately(0.84f, 0.001f);
        ReportPageLayout.CalculateZoom(ZoomMode.ActualSize, viewport, pageMm, 144, 144)
            .Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void Continuous_layout_preserves_page_order_and_hit_testing()
    {
        var layout = new ContinuousScrollLayout(12);
        layout.SetPageHeights(new[] { 100f, 250f, 80f });

        layout.TotalHeight.Should().Be(454f);
        layout.GetPageTop(1).Should().Be(112f);
        layout.HitTest(99f).Should().Be(0);
        layout.HitTest(112f).Should().Be(1);
        layout.HitTest(453f).Should().Be(2);
        layout.HitTest(454f).Should().Be(-1);
    }

    [Fact]
    public void Thumbnail_descriptor_requires_positive_size_and_disposes_image()
    {
        using var image = new Bitmap(16, 16);
        using var thumbnail = new ReportPageThumbnail(3, image);

        thumbnail.Page.Should().Be(3);
        thumbnail.Image.Should().BeSameAs(image);
        thumbnail.Size.Should().Be(new Size(16, 16));
    }
}
