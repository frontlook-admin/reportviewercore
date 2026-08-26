using System.Drawing;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using Xunit;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;

namespace Microsoft.ReportViewer.Design.Tests;

public sealed class RdlcDesignerCanvasTests
{
    [Fact]
    public void Report_point_conversion_is_zoom_and_dpi_safe()
    {
        var point = RdlcCanvasGeometry.ScreenToReport(new PointF(150, 125), new PointF(50, 25), 1.5f, 120f);
        Assert.Equal(.5556f, point.X, 3);
        Assert.Equal(.5556f, point.Y, 3);
    }

    [Fact]
    public void Snap_and_clamp_keep_items_inside_page()
    {
        var result = RdlcCanvasGeometry.SnapAndClamp(new RectangleF(-.2f, 1.13f, 2.17f, 1.01f), new SizeF(8.5f, 11), .25f);
        Assert.Equal(new RectangleF(0, 1.25f, 2.25f, 1f), result);
    }

    [Fact]
    public void Selection_supports_range_toggle_and_keyboard_navigation()
    {
        var state = new RdlcCanvasSelectionState(new[] { "A", "B", "C" });
        state.Select("B");
        state.Toggle("C");
        Assert.Equal(new[] { "B", "C" }, state.SelectedIds);
        Assert.Equal("B", state.MoveFocus(-1));
        state.SelectFocused();
        Assert.Equal(new[] { "B" }, state.SelectedIds);
    }

    [Fact]
    public void Alignment_and_distribution_are_deterministic()
    {
        var items = new[]
        {
            new RdlcCanvasItem("A", "Textbox", new RectangleF(1, 1, 1, 1)),
            new RdlcCanvasItem("B", "Textbox", new RectangleF(3, 2, 1, 1)),
            new RdlcCanvasItem("C", "Textbox", new RectangleF(5, 4, 1, 1))
        };
        RdlcCanvasLayout.AlignTop(items);
        RdlcCanvasLayout.DistributeVertical(items);
        Assert.All(items, item => Assert.Equal(1f, item.Bounds.Top));
        Assert.Equal(1f, items[0].Bounds.Left);
        Assert.Equal(3f, items[1].Bounds.Left);
        Assert.Equal(5f, items[2].Bounds.Left);
    }

    [Fact]
    public void Canvas_exposes_accessible_role_and_names()
    {
        using var canvas = new RdlcDesignerCanvas();
        canvas.Items.Add(new RdlcCanvasItem("Title", "Textbox", new RectangleF(1, 1, 2, .5f), "Report title"));
        canvas.SelectItem("Title");
        Assert.Equal("Report design canvas", canvas.AccessibleName);
        Assert.Equal(AccessibleRole.Client, canvas.AccessibleRole);
        Assert.Contains("Report title", canvas.GetAccessibleItemNames());
    }

    [Fact]
    public void ReportViewer_designer_exposes_smart_tag_and_legacy_verb()
    {
        using var viewer = new ReportViewerControl();
        using var designer = new ReportViewerDesigner();

        designer.Initialize(viewer);

        Assert.Single(designer.ActionLists);
        Assert.Single(designer.Verbs);
        var verb = designer.Verbs[0];
        Assert.NotNull(verb);
        Assert.Equal("Reset viewer appearance", verb!.Text);
        var actionItems = designer.ActionLists[0]!.GetSortedActionItems().Cast<DesignerActionItem>().ToList();
        Assert.Contains(actionItems, item =>
            item is DesignerActionMethodItem method &&
            method.DisplayName == "Reset viewer appearance");
    }
}
