using System.IO;
using System.Linq;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportSecurityPolicyTests
{
    [Fact]
    public void Default_policy_preserves_existing_report_execution_behavior()
    {
        var policy = ReportSecurityPolicy.Default;

        policy.EnforceTrustedReportDefinition.Should().BeFalse();
        policy.AllowCustomCode.Should().BeTrue();
        policy.AllowExternalImages.Should().BeTrue();
        policy.AllowHyperlinks.Should().BeTrue();
        policy.AllowAssemblies.Should().BeTrue();
    }

    [Fact]
    public void Restrictive_policy_reports_custom_code_images_links_and_assemblies()
    {
        const string definition = """
            <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition">
              <Code>Function Add(a, b) Return a + b End Function</Code>
              <CodeModules><CodeModule>Unsafe.Reporting.Extension</CodeModule></CodeModules>
              <Body><ReportItems>
                <Image><Source>External</Source><Value>https://example.invalid/logo.png</Value></Image>
                <Textbox><Action><Hyperlink>https://example.invalid</Hyperlink></Action></Textbox>
              </ReportItems></Body>
            </Report>
            """;

        var diagnostics = ReportSecurityPolicy.Restrictive.Analyze(definition).Diagnostics;

        diagnostics.Should().Contain(d => d.Feature == ReportSecurityFeature.CustomCode && d.Severity == ReportSecurityDiagnosticSeverity.Error);
        diagnostics.Should().Contain(d => d.Feature == ReportSecurityFeature.ExternalImages && d.Severity == ReportSecurityDiagnosticSeverity.Error);
        diagnostics.Should().Contain(d => d.Feature == ReportSecurityFeature.Hyperlinks && d.Severity == ReportSecurityDiagnosticSeverity.Error);
        diagnostics.Should().Contain(d => d.Feature == ReportSecurityFeature.Assemblies && d.Severity == ReportSecurityDiagnosticSeverity.Error);
    }

    [Fact]
    public void Restrictive_policy_requires_explicit_trust_for_definition_loading()
    {
        var report = new LocalReport
        {
            SecurityPolicy = ReportSecurityPolicy.Restrictive
        };

        var action = () => report.LoadReportDefinition(new StringReader("<Report />"));

        action.Should().Throw<ReportSecurityException>();
        report.SecurityDiagnostics.Should().Contain(d => d.Feature == ReportSecurityFeature.UntrustedReportDefinition);
    }

    [Fact]
    public void Explicitly_trusted_definition_can_be_loaded_under_restrictive_policy()
    {
        var report = new LocalReport
        {
            SecurityPolicy = ReportSecurityPolicy.Restrictive
        };

        report.LoadReportDefinition(new StringReader("<Report />"), isTrusted: true);

        report.SecurityDiagnostics.Should().NotContain(d => d.Severity == ReportSecurityDiagnosticSeverity.Error);
    }
}
