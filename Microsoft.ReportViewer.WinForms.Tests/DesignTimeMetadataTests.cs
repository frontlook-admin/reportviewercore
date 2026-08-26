using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing.Design;
using System.IO;
using System.Reflection;
using System.Windows.Forms.Design;
using FluentAssertions;
using Xunit;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;
using ReportViewerDesigner = Microsoft.Reporting.WinForms.ReportViewerDesigner;
using ReportViewerDesignerCodeDomSerializer = Microsoft.Reporting.WinForms.ReportViewerDesignerCodeDomSerializer;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class DesignTimeMetadataTests
{
    [Fact]
    public void Design_assembly_preserves_the_report_viewer_designer_contract()
    {
        var root = FindRepositoryRoot();
        var reportViewerSource = File.ReadAllText(Path.Combine(
            root,
            "Microsoft.ReportViewer.WinForms",
            "Microsoft.Reporting.WinForms",
            "ReportViewer.cs"));

        reportViewerSource.Should().Contain(
            "Microsoft.Reporting.WinForms.ReportViewerDesigner, Microsoft.ReportViewer.Design, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91");
    }

    [Fact]
    public void Design_assembly_declares_the_minimum_visual_studio_load_surface()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "Microsoft.ReportViewer.Design", "Microsoft.ReportViewer.Design.csproj");
        var designerPath = Path.Combine(root, "Microsoft.ReportViewer.Design", "Microsoft.Reporting.WinForms", "ReportViewerDesigner.cs");

        File.Exists(projectPath).Should().BeTrue();
        File.Exists(designerPath).Should().BeTrue();

        File.ReadAllText(projectPath).Should().Contain("<AssemblyName>Microsoft.ReportViewer.Design</AssemblyName>");
        File.ReadAllText(projectPath).Should().Contain("<AssemblyVersion>15.0.0.0</AssemblyVersion>");

        var designerSource = File.ReadAllText(designerPath);
        designerSource.Should().Contain("class ReportViewerDesigner : ControlDesigner");
        designerSource.Should().Contain("ReportViewerDesignerCodeDomSerializer");
    }

    [Fact]
    public void Design_assembly_exports_a_control_designer_and_code_dom_serializer()
    {
        typeof(ReportViewerDesigner).IsAssignableTo(typeof(ControlDesigner)).Should().BeTrue();
        typeof(ReportViewerDesignerCodeDomSerializer).IsAssignableTo(typeof(CodeDomSerializer)).Should().BeTrue();

        typeof(ReportViewerDesigner).Assembly.GetName().Name.Should().Be("Microsoft.ReportViewer.Design");
        typeof(ReportViewerDesigner).Assembly.GetName().Version.Should().Be(new Version(15, 0, 0, 0));
        typeof(ReportViewerDesigner).Assembly.GetName().GetPublicKeyToken()
            .Should().NotBeNullOrEmpty();
    }

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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ReportViewerCore.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
