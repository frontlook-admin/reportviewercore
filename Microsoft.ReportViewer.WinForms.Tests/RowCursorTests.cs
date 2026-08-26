using System.ComponentModel;
using System.Reflection;
using Microsoft.Reporting.WinForms;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class RowCursorTests
{
    [Fact]
    public void ReportViewer_ExposesPublicRowSelectionContract()
    {
        var selection = new ReportViewerRowSelection(2, 4, false, "tablix", 7, ["A", "B"]);

        selection.PageNumber.Should().Be(2);
        selection.RowIndex.Should().Be(4);
        selection.IsHeader.Should().BeFalse();
        selection.Source.Should().Be("tablix");
        selection.Cells.Should().Equal("A", "B");
    }

    [Fact]
    public void ReportViewer_ExposesRowSelectionChangedEventAndClipboardActions()
    {
        typeof(ReportViewerControl).GetEvent(nameof(ReportViewerControl.RowSelectionChanged))
            .Should().NotBeNull();
        typeof(ReportViewerControl).GetMethod(nameof(ReportViewerControl.CopySelectedCell))
            .Should().NotBeNull();
        typeof(ReportViewerControl).GetMethod(nameof(ReportViewerControl.CopySelectedRow))
            .Should().NotBeNull();
        typeof(ReportViewerControl).GetMethod(nameof(ReportViewerControl.CopySelectedTable))
            .Should().NotBeNull();
    }

    [Fact]
    public void ReportViewer_RowCursor_ExposesEnabledByDefaultOptOutProperty()
    {
        var property = typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.EnableRowCursor));

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(bool));
        property.GetCustomAttribute<DefaultValueAttribute>()!.Value.Should().Be(true);

        var internalViewerType = typeof(ReportViewerControl).Assembly
            .GetType("Microsoft.Reporting.WinForms.WinRSviewer");
        var internalProperty = internalViewerType!.GetProperty(nameof(ReportViewerControl.EnableRowCursor));

        internalProperty.Should().NotBeNull();
        internalProperty!.GetCustomAttribute<DesignerSerializationVisibilityAttribute>()!.Visibility
            .Should().Be(DesignerSerializationVisibility.Hidden);
    }

    [Fact]
    public void GdiPage_ResetSelectedRow_ClearsSelection()
    {
        var assembly = typeof(ReportViewerControl).Assembly;
        var rendererType = assembly.GetType("Microsoft.Reporting.WinForms.ClientGDIRenderer")!;
        var pageType = assembly.GetType("Microsoft.Reporting.WinForms.GdiPage")!;
        var reportType = assembly.GetType("Microsoft.Reporting.WinForms.RenderingReport")!;
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        var report = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(reportType);
        var addTarget = reportType.GetMethod("AddTablixRowTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        addTarget.Invoke(report, [new System.Drawing.RectangleF(10, 10, 100, 8), false, 0, "row", 0]);
        rendererType.GetField("m_report", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(renderer, report);
        var page = Activator.CreateInstance(pageType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [renderer], null)!;
        var move = pageType.GetMethod("MoveSelectedRow", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var reset = pageType.GetMethod("ResetSelectedRow", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var selected = pageType.GetProperty("SelectedRowTargetIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;

        move.Invoke(page, [false]);
        selected.GetValue(page).Should().Be(0);

        reset.Invoke(page, null);

        selected.GetValue(page).Should().Be(-1);
    }

    [Fact]
    public void GdiPage_SelectRowAtPoint_ChangesSelectionToClickedRow()
    {
        var assembly = typeof(ReportViewerControl).Assembly;
        var rendererType = assembly.GetType("Microsoft.Reporting.WinForms.ClientGDIRenderer")!;
        var pageType = assembly.GetType("Microsoft.Reporting.WinForms.GdiPage")!;
        var reportType = assembly.GetType("Microsoft.Reporting.WinForms.RenderingReport")!;
        var renderer = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(rendererType);
        var report = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(reportType);
        var addTarget = reportType.GetMethod("AddTablixRowTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        addTarget.Invoke(report, [new System.Drawing.RectangleF(10, 10, 100, 8), false, 0, "row", 0]);
        addTarget.Invoke(report, [new System.Drawing.RectangleF(10, 20, 100, 8), false, 1, "row", 1]);
        rendererType.GetField("m_report", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(renderer, report);
        var page = Activator.CreateInstance(pageType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [renderer], null)!;
        var select = pageType.GetMethod("SelectRowAtPoint", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var selected = pageType.GetProperty("SelectedRowTargetIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;

        select.Invoke(page, [new System.Drawing.PointF(20, 24)]).Should().Be(true);

        selected.GetValue(page).Should().Be(1);
    }
}
