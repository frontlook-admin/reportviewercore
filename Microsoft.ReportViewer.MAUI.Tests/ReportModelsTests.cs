using FluentAssertions;
using Microsoft.ReportViewer.MAUI.Models;
using Xunit;

namespace Microsoft.ReportViewer.MAUI.Tests;

/// <summary>
/// Unit tests for the ReportModels classes.
/// </summary>
public class ReportModelsTests
{
    [Fact]
    public void NavigationContracts_ShouldPreserveTargetsAndProgressBounds()
    {
        var node = new ReportDocumentMapNode { Id = "root", Label = "Summary", Page = 2 };
        var search = new ReportSearchEventArgs("total") { MatchPage = 2 };
        var bookmark = new ReportBookmarkEventArgs("summary") { Page = 2 };
        var drillthrough = new ReportDrillthroughEventArgs("Detail", new Dictionary<string, string> { ["Id"] = "7" });
        var progress = new ReportExportProgressEventArgs("PDF", 2.0, true);

        node.Label.Should().Be("Summary");
        search.MatchPage.Should().Be(2);
        bookmark.Page.Should().Be(2);
        drillthrough.Parameters["Id"].Should().Be("7");
        progress.Progress.Should().Be(1);
        progress.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void AccessibilitySettings_ShouldProvideStableAutomationDefaults()
    {
        var settings = new ReportAccessibilitySettings();

        settings.ViewerAutomationName.Should().Be("Report viewer");
        settings.StatusAutomationName.Should().Be("Report status");
        settings.ReduceMotion.Should().BeFalse();
    }

    #region ReportDataSourceInfo Tests

    [Fact]
    public void ReportDataSourceInfo_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var dataSource = new ReportDataSourceInfo("TestDataSource", new[] { 1, 2, 3 });

        // Assert
        dataSource.Name.Should().Be("TestDataSource");
        dataSource.Value.Should().NotBeNull();
    }

    [Fact]
    public void ReportDataSourceInfo_WithEmptyName_ShouldAccept()
    {
        // Arrange & Act
        var dataSource = new ReportDataSourceInfo("", new object());

        // Assert
        dataSource.Name.Should().BeEmpty();
    }

    [Fact]
    public void ReportDataSourceInfo_WithNullValue_ShouldAccept()
    {
        // Arrange & Act
        var dataSource = new ReportDataSourceInfo("TestDS", null!);

        // Assert
        dataSource.Value.Should().BeNull();
    }

    #endregion

    #region ReportRenderingCompleteEventArgs Tests

    [Fact]
    public void ReportRenderingCompleteEventArgs_Success_ShouldSetProperties()
    {
        // Arrange & Act
        var args = new ReportRenderingCompleteEventArgs(true, 10);

        // Assert
        args.Success.Should().BeTrue();
        args.TotalPages.Should().Be(10);
        args.Error.Should().BeNull();
    }

    [Fact]
    public void ReportRenderingCompleteEventArgs_Failure_ShouldSetProperties()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        var args = new ReportRenderingCompleteEventArgs(false, 0, exception);

        // Assert
        args.Success.Should().BeFalse();
        args.TotalPages.Should().Be(0);
        args.Error.Should().BeSameAs(exception);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void ReportRenderingCompleteEventArgs_ShouldAcceptVariousPageCounts(int pageCount)
    {
        // Arrange & Act
        var args = new ReportRenderingCompleteEventArgs(true, pageCount);

        // Assert
        args.TotalPages.Should().Be(pageCount);
    }

    #endregion

    #region ReportErrorEventArgs Tests

    [Fact]
    public void ReportErrorEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange
        var exception = new ArgumentException("Test exception");

        // Act
        var args = new ReportErrorEventArgs(exception);

        // Assert
        args.Exception.Should().BeSameAs(exception);
        args.Handled.Should().BeFalse();
    }

    [Fact]
    public void ReportErrorEventArgs_Handled_ShouldBeSettable()
    {
        // Arrange
        var args = new ReportErrorEventArgs(new Exception());

        // Act
        args.Handled = true;

        // Assert
        args.Handled.Should().BeTrue();
    }

    [Fact]
    public void ReportErrorEventArgs_ShouldWorkWithDifferentExceptionTypes()
    {
        // Arrange & Act & Assert
        var args1 = new ReportErrorEventArgs(new ArgumentNullException("param"));
        args1.Exception.Should().BeOfType<ArgumentNullException>();

        var args2 = new ReportErrorEventArgs(new InvalidOperationException());
        args2.Exception.Should().BeOfType<InvalidOperationException>();

        var args3 = new ReportErrorEventArgs(new FormatException());
        args3.Exception.Should().BeOfType<FormatException>();
    }

    #endregion

    #region ReportExportRequestedEventArgs Tests

    [Theory]
    [InlineData("PDF")]
    [InlineData("EXCELOPENXML")]
    [InlineData("WORDOPENXML")]
    [InlineData("IMAGE")]
    public void ReportExportRequestedEventArgs_ShouldAcceptVariousFormats(string format)
    {
        // Arrange & Act
        var args = new ReportExportRequestedEventArgs(format);

        // Assert
        args.Format.Should().Be(format);
        args.Cancel.Should().BeFalse();
    }

    [Fact]
    public void ReportExportRequestedEventArgs_Cancel_ShouldBeSettable()
    {
        // Arrange
        var args = new ReportExportRequestedEventArgs("PDF");

        // Act
        args.Cancel = true;

        // Assert
        args.Cancel.Should().BeTrue();
    }

    #endregion

    #region ReportPrintRequestedEventArgs Tests

    [Fact]
    public void ReportPrintRequestedEventArgs_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var args = new ReportPrintRequestedEventArgs();

        // Assert
        args.Cancel.Should().BeFalse();
        args.Copies.Should().Be(1);
        args.FromPage.Should().Be(1);
        args.ToPage.Should().Be(int.MaxValue);
    }

    [Fact]
    public void ReportPrintRequestedEventArgs_ShouldAllowSettingPageRange()
    {
        // Arrange
        var args = new ReportPrintRequestedEventArgs();

        // Act
        args.FromPage = 5;
        args.ToPage = 10;
        args.Copies = 3;

        // Assert
        args.FromPage.Should().Be(5);
        args.ToPage.Should().Be(10);
        args.Copies.Should().Be(3);
    }

    #endregion

    #region ReportNavigationEventArgs Tests

    [Fact]
    public void ReportNavigationEventArgs_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var args = new ReportNavigationEventArgs(5, 20);

        // Assert
        args.CurrentPage.Should().Be(5);
        args.TotalPages.Should().Be(20);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 100)]
    [InlineData(50, 50)]
    public void ReportNavigationEventArgs_ShouldAcceptVariousPageNumbers(int current, int total)
    {
        // Arrange & Act
        var args = new ReportNavigationEventArgs(current, total);

        // Assert
        args.CurrentPage.Should().Be(current);
        args.TotalPages.Should().Be(total);
    }

    #endregion

    #region ReportViewerSettings Tests

    [Fact]
    public void ReportViewerSettings_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var settings = new ReportViewerSettings();

        // Assert
        settings.ShowToolbar.Should().BeTrue();
        settings.ShowExportButton.Should().BeTrue();
        settings.ShowPrintButton.Should().BeTrue();
        settings.ShowRefreshButton.Should().BeTrue();
        settings.ShowZoomControls.Should().BeTrue();
        settings.ShowPageNavigator.Should().BeTrue();
        settings.ZoomLevel.Should().Be(100);
        settings.AutoRefresh.Should().BeFalse();
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    [InlineData(200)]
    public void ReportViewerSettings_ZoomLevel_ShouldAcceptValidValues(int zoomLevel)
    {
        // Arrange
        var settings = new ReportViewerSettings();

        // Act
        settings.ZoomLevel = zoomLevel;

        // Assert
        settings.ZoomLevel.Should().Be(zoomLevel);
    }

    [Fact]
    public void ReportViewerSettings_AllProperties_ShouldBeSettable()
    {
        // Arrange
        var settings = new ReportViewerSettings();

        // Act
        settings.ShowToolbar = false;
        settings.ShowExportButton = false;
        settings.ShowPrintButton = false;
        settings.ShowRefreshButton = false;
        settings.ShowZoomControls = false;
        settings.ShowPageNavigator = false;
        settings.AutoRefresh = true;
        settings.ZoomLevel = 75;

        // Assert
        settings.ShowToolbar.Should().BeFalse();
        settings.ShowExportButton.Should().BeFalse();
        settings.ShowPrintButton.Should().BeFalse();
        settings.ShowRefreshButton.Should().BeFalse();
        settings.ShowZoomControls.Should().BeFalse();
        settings.ShowPageNavigator.Should().BeFalse();
        settings.AutoRefresh.Should().BeTrue();
        settings.ZoomLevel.Should().Be(75);
    }

    #endregion
}
