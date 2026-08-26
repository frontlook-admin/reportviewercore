using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Reporting.WinForms;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class P1CFeatureTests
{
    [Fact]
    public void SearchMatchInfo_exposes_rich_match_metadata()
    {
        var match = new SearchMatchInfo("needle", 3, 2, new PointF(12, 24));

        match.Text.Should().Be("needle");
        match.PageNumber.Should().Be(3);
        match.MatchIndex.Should().Be(2);
        match.Point.Should().Be(new PointF(12, 24));
    }

    [Fact]
    public void ReportViewer_exposes_search_match_event_and_metadata()
    {
        typeof(ReportViewerControl).GetEvent(nameof(ReportViewerControl.SearchMatchChanged)).Should().NotBeNull();
        typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.SearchMatches)).Should().NotBeNull();
    }

    [Fact]
    public void ReportViewer_report_scoped_preference_autosave_is_opt_in()
    {
        var autoSave = typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.AutoSaveReportPreferences));
        var key = typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.ReportPreferenceKey));

        autoSave.Should().NotBeNull();
        autoSave!.PropertyType.Should().Be(typeof(bool));
        autoSave.GetCustomAttribute<DefaultValueAttribute>()!.Value.Should().Be(false);
        key.Should().NotBeNull();
        key!.PropertyType.Should().Be(typeof(string));
        key.GetCustomAttribute<DesignerSerializationVisibilityAttribute>()!.Visibility
            .Should().Be(DesignerSerializationVisibility.Hidden);
    }

    [Fact]
    public void DocumentMapNode_filter_preserves_matching_ancestors_and_descendants()
    {
        var nodeType = typeof(DocumentMapNode);
        var constructor = nodeType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
            [typeof(string), typeof(string), typeof(DocumentMapNode[])], null)!;
        var root = (DocumentMapNode)constructor.Invoke(["Root", "root", new[]
        {
            (DocumentMapNode)constructor.Invoke(["Sales", "sales", null]),
            (DocumentMapNode)constructor.Invoke(["Sales Detail", "detail", null]),
            (DocumentMapNode)constructor.Invoke(["Other", "other", null])
        }]);

        var filtered = root.Filter("detail");

        filtered.Should().NotBeNull();
        filtered!.Label.Should().Be("Root");
        filtered.Children.Should().ContainSingle(node => node.Id == "detail");
    }
}
