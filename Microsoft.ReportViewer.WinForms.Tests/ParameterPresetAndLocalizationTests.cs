using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ParameterPresetAndLocalizationTests
{
    [Fact]
    public void ParameterPreset_RoundTripsValuesWithoutSharingMutableCollections()
    {
        var preset = new ReportViewerParameterPreset("Quarterly", new Dictionary<string, string[]>
        {
            ["Region"] = ["West", "North"]
        });

        var restored = ReportViewerParameterPreset.FromJson(preset.ToJson());
        restored.Name.Should().Be("Quarterly");
        restored.Values["Region"].Should().Equal("West", "North");

        preset.Values["Region"][0] = "Changed";
        restored.Values["Region"][0].Should().Be("West");
    }

    [Fact]
    public void ParameterPreset_RejectsSecretLikeParametersBeforePersistence()
    {
        var preset = ReportViewerParameterPreset.CreateSafe("Default", new Dictionary<string, string[]>
        {
            ["Region"] = ["West"],
            ["Password"] = ["should-not-persist"],
            ["ApiToken"] = ["also-secret"]
        });

        preset.Values.Keys.Should().Equal("Region");
        preset.ToJson().Should().NotContain("should-not-persist");
        preset.ToJson().Should().NotContain("also-secret");
    }

    [Fact]
    public void Preferences_DefaultsDoNotPersistParameterPresetsOrReportValues()
    {
        var preferences = new ReportViewerPreferences();

        preferences.ParameterPresets.Should().BeEmpty();
        preferences.ToJson().Should().NotContain("Password");
        preferences.ToJson().Should().NotContain("Region");
    }

    [Fact]
    public void RenderingExtensionLocalization_FallsBackToBuiltInNameWhenHostDoesNotProvideOne()
    {
        var extension = (RenderingExtension)Activator.CreateInstance(
            typeof(RenderingExtension),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            args: new object[] { "PDF", "PDF", true },
            culture: null)!;
        var localized = LocalizationHelperForTests.GetLocalizedName(extension);

        localized.Should().Be("PDF");
    }
}

internal static class LocalizationHelperForTests
{
    public static string GetLocalizedName(RenderingExtension extension)
    {
        var helperType = typeof(Microsoft.Reporting.WinForms.ReportViewer).Assembly.GetType("Microsoft.Reporting.WinForms.LocalizationHelper", throwOnError: true);
        var method = helperType!.GetMethod("GetLocalizedNameForRenderingExtension", new[] { typeof(RenderingExtension) });
        return (string)method!.Invoke(helperType.GetProperty("Current")!.GetValue(null), new object[] { extension })!;
    }
}
