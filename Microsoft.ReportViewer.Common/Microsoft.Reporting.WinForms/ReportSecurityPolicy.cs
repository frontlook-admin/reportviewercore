using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Reporting.WinForms;

public enum ReportSecurityFeature
{
    CustomCode,
    ExternalImages,
    Hyperlinks,
    Assemblies,
    UntrustedReportDefinition
}

public enum ReportSecurityDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed class ReportSecurityDiagnostic
{
    public ReportSecurityDiagnostic(ReportSecurityFeature feature, ReportSecurityDiagnosticSeverity severity, string message)
    {
        Feature = feature;
        Severity = severity;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public ReportSecurityFeature Feature { get; }
    public ReportSecurityDiagnosticSeverity Severity { get; }
    public string Message { get; }
}

public sealed class ReportSecurityAnalysis
{
    internal ReportSecurityAnalysis(IReadOnlyList<ReportSecurityDiagnostic> diagnostics) => Diagnostics = diagnostics;

    public IReadOnlyList<ReportSecurityDiagnostic> Diagnostics { get; }
    public bool IsAllowed => Diagnostics.All(diagnostic => diagnostic.Severity != ReportSecurityDiagnosticSeverity.Error);
}

/// <summary>
/// Restriction and diagnostics contract for report features that can execute code or access external resources.
/// This is not an in-process sandbox or isolation boundary.
/// </summary>
public sealed class ReportSecurityPolicy
{
    public static ReportSecurityPolicy Default => new();

    public static ReportSecurityPolicy Restrictive => new()
    {
        EnforceTrustedReportDefinition = true,
        AllowCustomCode = false,
        AllowExternalImages = false,
        AllowHyperlinks = false,
        AllowAssemblies = false
    };

    public bool EnforceTrustedReportDefinition { get; set; }
    public bool AllowCustomCode { get; set; } = true;
    public bool AllowExternalImages { get; set; } = true;
    public bool AllowHyperlinks { get; set; } = true;
    public bool AllowAssemblies { get; set; } = true;

    public ReportSecurityAnalysis Analyze(string reportDefinition, bool isTrusted = false)
    {
        ArgumentNullException.ThrowIfNull(reportDefinition);
        var diagnostics = new List<ReportSecurityDiagnostic>();

        if (EnforceTrustedReportDefinition && !isTrusted)
        {
            diagnostics.Add(new ReportSecurityDiagnostic(
                ReportSecurityFeature.UntrustedReportDefinition,
                ReportSecurityDiagnosticSeverity.Error,
                "The report definition is not marked trusted. Load it through the trusted-report boundary or use an isolated process."));
        }

        var document = XDocument.Parse(reportDefinition, LoadOptions.PreserveWhitespace);
        var elements = document.Descendants();
        AddFeatureDiagnostic(diagnostics, ReportSecurityFeature.CustomCode,
            elements.Any(element => element.Name.LocalName == "Code" && !string.IsNullOrWhiteSpace(element.Value)),
            AllowCustomCode, "The report contains custom code that may execute during report processing.");
        AddFeatureDiagnostic(diagnostics, ReportSecurityFeature.ExternalImages,
            elements.Any(element => element.Name.LocalName == "Image" && element.Elements().Any(child => child.Name.LocalName == "Source" && string.Equals(child.Value.Trim(), "External", StringComparison.OrdinalIgnoreCase))),
            AllowExternalImages, "The report references external images and may perform network or file access.");
        AddFeatureDiagnostic(diagnostics, ReportSecurityFeature.Hyperlinks,
            elements.Any(element => element.Name.LocalName == "Hyperlink" && !string.IsNullOrWhiteSpace(element.Value)),
            AllowHyperlinks, "The report contains hyperlinks that may navigate outside the host application.");
        AddFeatureDiagnostic(diagnostics, ReportSecurityFeature.Assemblies,
            elements.Any(element => element.Name.LocalName is "CodeModule" or "CodeModules" && !string.IsNullOrWhiteSpace(element.Value)),
            AllowAssemblies, "The report references custom assemblies that are loaded into the report execution environment.");

        return new ReportSecurityAnalysis(diagnostics);
    }

    private static void AddFeatureDiagnostic(ICollection<ReportSecurityDiagnostic> diagnostics, ReportSecurityFeature feature, bool present, bool allowed, string message)
    {
        if (present)
        {
            diagnostics.Add(new ReportSecurityDiagnostic(feature,
                allowed ? ReportSecurityDiagnosticSeverity.Warning : ReportSecurityDiagnosticSeverity.Error, message));
        }
    }
}