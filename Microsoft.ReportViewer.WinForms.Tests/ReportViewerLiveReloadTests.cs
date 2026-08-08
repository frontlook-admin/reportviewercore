using Microsoft.Reporting.WinForms;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportViewerLiveReloadTests
{
    [Fact]
    public void LiveReloadOptions_DefaultToDisabled()
    {
        var options = new ReportViewerLiveReloadOptions();

        options.Enabled.Should().BeFalse();
        options.PollInterval.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void LiveReloadOptions_ExposeConfiguredSourcesAndCallback()
    {
        var definitionPath = Path.Combine(Path.GetTempPath(), "sample.rdlc");
        var dataPath = Path.Combine(Path.GetTempPath(), "sample.json");
        Func<CancellationToken, ValueTask<ReportViewerReloadSnapshot>> callback =
            _ => ValueTask.FromResult(new ReportViewerReloadSnapshot(
                [1, 2, 3],
                []));
        var options = new ReportViewerLiveReloadOptions
        {
            Enabled = true,
            ReportDefinitionPath = definitionPath,
            DataFilePaths = [dataPath],
            PollInterval = TimeSpan.FromSeconds(3),
            DebounceDelay = TimeSpan.FromMilliseconds(100),
            StabilityDelay = TimeSpan.FromMilliseconds(50),
            MaxStableReadAttempts = 5,
            ReloadAsync = callback
        };

        options.Enabled.Should().BeTrue();
        options.ReportDefinitionPath.Should().Be(definitionPath);
        options.DataFilePaths.Should().ContainSingle().Which.Should().Be(dataPath);
        options.PollInterval.Should().Be(TimeSpan.FromSeconds(3));
        options.ReloadAsync.Should().BeSameAs(callback);
    }
}
