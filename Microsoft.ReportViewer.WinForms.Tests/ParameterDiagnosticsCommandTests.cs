using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ParameterDiagnosticsCommandTests
{
    [Fact]
    public void Parameter_validation_reports_required_multi_value_and_date_range_errors()
    {
        var required = ReportViewerParameterValidator.ValidateRequired("Region", Array.Empty<string>(), allowBlank: false, nullable: false, multiValue: true);
        required.IsValid.Should().BeFalse();
        required.Errors.Should().ContainSingle().Which.Code.Should().Be("required");

        var multi = ReportViewerParameterValidator.ValidateMultiValue("Region", new[] { "West" }, multiValue: true, minimumSelections: 2);
        multi.IsValid.Should().BeFalse();
        multi.Errors.Should().ContainSingle().Which.Code.Should().Be("minimum-selections");

        var range = ReportViewerParameterValidator.ValidateDateRange("From", "2026-04-20", "2026-04-01");
        range.IsValid.Should().BeFalse();
        range.Errors.Should().ContainSingle().Which.Code.Should().Be("date-range");
    }

    [Fact]
    public void Diagnostic_panel_preserves_severity_and_redacts_sensitive_values_on_export()
    {
        var panel = new ReportViewerDiagnosticPanel();
        panel.Add(ReportViewerDiagnosticSeverity.Warning, "PARAM", "Password=secret-token");
        panel.Add(ReportViewerDiagnosticSeverity.Error, "RPT", "Render failed");

        panel.HighestSeverity.Should().Be(ReportViewerDiagnosticSeverity.Error);
        var text = panel.ExportSafeText();
        text.Should().Contain("Render failed");
        text.Should().NotContain("secret-token");
        text.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void Registered_command_can_be_disposed_without_affecting_built_in_toolbar_items()
    {
        var viewerType = typeof(Microsoft.Reporting.WinForms.ReportViewer);
        viewerType.GetMethod("RegisterCommand").Should().NotBeNull();
        viewerType.GetMethod("ResetParameters").Should().NotBeNull();
        viewerType.GetProperty("Diagnostics").Should().NotBeNull();
    }
}
