using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Reflection;
using FluentAssertions;
using Xunit;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class DesignTimeMetadataTests
{
    [Fact]
    public void ReportViewer_is_advertised_as_a_toolbox_control_with_stable_default_property_and_event()
    {
        var type = typeof(ReportViewerControl);

        type.GetCustomAttributes(typeof(ToolboxItemAttribute), inherit: true)
            .Cast<ToolboxItemAttribute>()
            .Single()
            .ToolboxItemTypeName
            .Should().Be(new ToolboxItemAttribute(true).ToolboxItemTypeName);
        type.GetCustomAttributes(typeof(DefaultPropertyAttribute), inherit: true)
            .Cast<DefaultPropertyAttribute>()
            .Single()
            .Name.Should().Be(nameof(ReportViewerControl.LocalReport));
        type.GetCustomAttributes(typeof(DefaultEventAttribute), inherit: true)
            .Cast<DefaultEventAttribute>()
            .Single()
            .Name.Should().Be(nameof(ReportViewerControl.ReportError));
        type.GetCustomAttributes(typeof(DesignerAttribute), inherit: true)
            .Cast<DesignerAttribute>()
            .Single(attribute => attribute.DesignerTypeName.Contains("Microsoft.ReportViewer.Design"))
            .DesignerTypeName.Should().Contain("Microsoft.ReportViewer.Design");
    }

    [Fact]
    public void ReportViewer_report_models_are_content_serialized_and_runtime_state_is_hidden()
    {
        typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.LocalReport))!
            .GetCustomAttribute<DesignerSerializationVisibilityAttribute>()!
            .Visibility.Should().Be(DesignerSerializationVisibility.Content);
        typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.ServerReport))!
            .GetCustomAttribute<DesignerSerializationVisibilityAttribute>()!
            .Visibility.Should().Be(DesignerSerializationVisibility.Content);
        typeof(ReportViewerControl).GetProperty(nameof(ReportViewerControl.Theme))!
            .GetCustomAttribute<DesignerSerializationVisibilityAttribute>()!
            .Visibility.Should().Be(DesignerSerializationVisibility.Hidden);
    }
}
