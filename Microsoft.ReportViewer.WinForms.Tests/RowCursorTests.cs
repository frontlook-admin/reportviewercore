using System.ComponentModel;
using System.Reflection;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class RowCursorTests
{
    [Fact]
    public void ReportViewer_RowCursor_ExposesEnabledByDefaultOptOutProperty()
    {
        var property = typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.EnableRowCursor));

        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(bool));
        property.GetCustomAttribute<DefaultValueAttribute>()!.Value.Should().Be(true);
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
}
