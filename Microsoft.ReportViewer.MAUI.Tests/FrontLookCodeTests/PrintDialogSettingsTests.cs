using FluentAssertions;
using Microsoft.ReportViewer.MAUI.FrontLookCode;
using System.Text.Json;
using Xunit;

namespace Microsoft.ReportViewer.MAUI.Tests.FrontLookCodeTests;

/// <summary>
/// Unit tests for the PrintDialogSettings class.
/// Mirrors the WinForms tests for cross-platform compatibility validation.
/// </summary>
public class PrintDialogSettingsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var settings = new PrintDialogSettings();

        // Assert
        settings.PrinterName.Should().BeEmpty();
        settings.Copies.Should().Be(1);
        settings.PrintRange.Should().Be(PrintRangeType.AllPages);
        settings.FromPage.Should().Be(1);
        settings.ToPage.Should().Be(1);
        settings.Collate.Should().BeFalse();
        settings.Duplex.Should().Be(DuplexType.Default);
        settings.PrintToFile.Should().BeFalse();
        settings.PageSettings.Should().NotBeNull();
    }

    #endregion

    #region Property Tests

    [Theory]
    [InlineData("Microsoft Print to PDF")]
    [InlineData("HP LaserJet Pro")]
    [InlineData("")]
    public void PrinterName_ShouldAcceptVariousValues(string printerName)
    {
        // Arrange
        var settings = new PrintDialogSettings();

        // Act
        settings.PrinterName = printerName;

        // Assert
        settings.PrinterName.Should().Be(printerName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Copies_ShouldAcceptPositiveValues(short copies)
    {
        // Arrange
        var settings = new PrintDialogSettings();

        // Act
        settings.Copies = copies;

        // Assert
        settings.Copies.Should().Be(copies);
    }

    [Theory]
    [InlineData(PrintRangeType.AllPages)]
    [InlineData(PrintRangeType.SomePages)]
    [InlineData(PrintRangeType.Selection)]
    [InlineData(PrintRangeType.CurrentPage)]
    public void PrintRange_ShouldAcceptAllEnumValues(PrintRangeType printRange)
    {
        // Arrange
        var settings = new PrintDialogSettings();

        // Act
        settings.PrintRange = printRange;

        // Assert
        settings.PrintRange.Should().Be(printRange);
    }

    [Theory]
    [InlineData(DuplexType.Default)]
    [InlineData(DuplexType.Simplex)]
    [InlineData(DuplexType.Horizontal)]
    [InlineData(DuplexType.Vertical)]
    public void Duplex_ShouldAcceptAllEnumValues(DuplexType duplex)
    {
        // Arrange
        var settings = new PrintDialogSettings();

        // Act
        settings.Duplex = duplex;

        // Assert
        settings.Duplex.Should().Be(duplex);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(5, 15)]
    [InlineData(1, 1)]
    public void PageRange_ShouldAcceptValidRanges(int fromPage, int toPage)
    {
        // Arrange
        var settings = new PrintDialogSettings();

        // Act
        settings.FromPage = fromPage;
        settings.ToPage = toPage;

        // Assert
        settings.FromPage.Should().Be(fromPage);
        settings.ToPage.Should().Be(toPage);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        // Arrange
        var settings = new PrintDialogSettings
        {
            PrinterName = "Test Printer",
            Copies = 3,
            PrintRange = PrintRangeType.SomePages,
            FromPage = 2,
            ToPage = 5,
            Collate = true,
            Duplex = DuplexType.Horizontal
        };

        // Act
        var json = settings.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Test Printer");
        json.Should().Contain("SomePages");
    }

    [Fact]
    public void FromJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var original = new PrintDialogSettings
        {
            PrinterName = "Test Printer",
            Copies = 3,
            PrintRange = PrintRangeType.SomePages,
            FromPage = 2,
            ToPage = 5,
            Collate = true,
            Duplex = DuplexType.Horizontal,
            PageSettings = new PageSettingsModel
            {
                Landscape = true,
                Color = true,
                Margins = new MarginsModel(50, 50, 50, 50),
                PaperSize = new PaperSizeModel("A4", 827, 1169)
            }
        };

        var json = original.ToJson();

        // Act
        var deserialized = PrintDialogSettings.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PrinterName.Should().Be("Test Printer");
        deserialized.Copies.Should().Be(3);
        deserialized.PrintRange.Should().Be(PrintRangeType.SomePages);
        deserialized.FromPage.Should().Be(2);
        deserialized.ToPage.Should().Be(5);
        deserialized.Collate.Should().BeTrue();
        deserialized.Duplex.Should().Be(DuplexType.Horizontal);
        deserialized.PageSettings.Landscape.Should().BeTrue();
        deserialized.PageSettings.Color.Should().BeTrue();
    }

    [Fact]
    public void FromJson_WithNullOrEmpty_ShouldReturnNull()
    {
        // Act & Assert
        PrintDialogSettings.FromJson(null!).Should().BeNull();
        PrintDialogSettings.FromJson("").Should().BeNull();
        PrintDialogSettings.FromJson("   ").Should().BeNull();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var act = () => PrintDialogSettings.FromJson(invalidJson);

        // Assert
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void JsonRoundTrip_ShouldPreserveAllValues()
    {
        // Arrange
        var original = CreateFullyPopulatedSettings();

        // Act
        var json = original.ToJson();
        var deserialized = PrintDialogSettings.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PrinterName.Should().Be(original.PrinterName);
        deserialized.Copies.Should().Be(original.Copies);
        deserialized.PrintRange.Should().Be(original.PrintRange);
        deserialized.FromPage.Should().Be(original.FromPage);
        deserialized.ToPage.Should().Be(original.ToPage);
        deserialized.Collate.Should().Be(original.Collate);
        deserialized.Duplex.Should().Be(original.Duplex);
        deserialized.PrintToFile.Should().Be(original.PrintToFile);
    }

    #endregion

    #region PageSettingsModel Tests

    [Fact]
    public void PageSettingsModel_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var pageSettings = new PageSettingsModel();

        // Assert
        pageSettings.Landscape.Should().BeFalse();
        pageSettings.Color.Should().BeTrue();
        pageSettings.Margins.Should().NotBeNull();
        pageSettings.PaperSize.Should().NotBeNull();
    }

    [Fact]
    public void PageSettingsModel_Landscape_ShouldAcceptBooleanValues()
    {
        // Arrange
        var pageSettings = new PageSettingsModel();

        // Act & Assert
        pageSettings.Landscape = true;
        pageSettings.Landscape.Should().BeTrue();

        pageSettings.Landscape = false;
        pageSettings.Landscape.Should().BeFalse();
    }

    #endregion

    #region MarginsModel Tests

    [Fact]
    public void MarginsModel_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var margins = new MarginsModel();

        // Assert
        margins.Left.Should().Be(100);
        margins.Right.Should().Be(100);
        margins.Top.Should().Be(100);
        margins.Bottom.Should().Be(100);
    }

    [Fact]
    public void MarginsModel_ParameterizedConstructor_ShouldSetValues()
    {
        // Arrange & Act
        var margins = new MarginsModel(50, 60, 70, 80);

        // Assert
        margins.Left.Should().Be(50);
        margins.Right.Should().Be(60);
        margins.Top.Should().Be(70);
        margins.Bottom.Should().Be(80);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(100, 100, 100, 100)]
    [InlineData(25, 50, 75, 100)]
    public void MarginsModel_ShouldAcceptVariousValues(int left, int right, int top, int bottom)
    {
        // Arrange & Act
        var margins = new MarginsModel(left, right, top, bottom);

        // Assert
        margins.Left.Should().Be(left);
        margins.Right.Should().Be(right);
        margins.Top.Should().Be(top);
        margins.Bottom.Should().Be(bottom);
    }

    #endregion

    #region PaperSizeModel Tests

    [Fact]
    public void PaperSizeModel_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var paperSize = new PaperSizeModel();

        // Assert
        paperSize.PaperName.Should().Be("Letter");
        paperSize.Width.Should().Be(850);
        paperSize.Height.Should().Be(1100);
    }

    [Fact]
    public void PaperSizeModel_ParameterizedConstructor_ShouldSetValues()
    {
        // Arrange & Act
        var paperSize = new PaperSizeModel("A4", 827, 1169);

        // Assert
        paperSize.PaperName.Should().Be("A4");
        paperSize.Width.Should().Be(827);
        paperSize.Height.Should().Be(1169);
    }

    [Theory]
    [InlineData("Letter", 850, 1100)]
    [InlineData("A4", 827, 1169)]
    [InlineData("Legal", 850, 1400)]
    [InlineData("A3", 1169, 1654)]
    public void PaperSizeModel_ShouldAcceptStandardPaperSizes(string name, int width, int height)
    {
        // Arrange & Act
        var paperSize = new PaperSizeModel(name, width, height);

        // Assert
        paperSize.PaperName.Should().Be(name);
        paperSize.Width.Should().Be(width);
        paperSize.Height.Should().Be(height);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_ShouldCreateDeepCopy()
    {
        // Arrange
        var original = CreateFullyPopulatedSettings();

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.PrinterName.Should().Be(original.PrinterName);
        cloned.Copies.Should().Be(original.Copies);
        cloned.PageSettings.Should().NotBeSameAs(original.PageSettings);
        cloned.PageSettings.Margins.Should().NotBeSameAs(original.PageSettings.Margins);
        cloned.PageSettings.PaperSize.Should().NotBeSameAs(original.PageSettings.PaperSize);
    }

    [Fact]
    public void Clone_ModifyingClone_ShouldNotAffectOriginal()
    {
        // Arrange
        var original = CreateFullyPopulatedSettings();
        var cloned = original.Clone();

        // Act
        cloned.PrinterName = "Modified Printer";
        cloned.Copies = 99;
        cloned.PageSettings.Landscape = !original.PageSettings.Landscape;

        // Assert
        original.PrinterName.Should().NotBe("Modified Printer");
        original.Copies.Should().NotBe(99);
    }

    #endregion

    #region Helper Methods

    private static PrintDialogSettings CreateFullyPopulatedSettings()
    {
        return new PrintDialogSettings
        {
            PrinterName = "Test Printer XL-500",
            Copies = 5,
            PrintRange = PrintRangeType.SomePages,
            FromPage = 3,
            ToPage = 15,
            Collate = true,
            Duplex = DuplexType.Vertical,
            PrintToFile = false,
            PageSettings = new PageSettingsModel
            {
                Landscape = true,
                Color = true,
                Margins = new MarginsModel(75, 75, 50, 50),
                PaperSize = new PaperSizeModel("A4", 827, 1169)
            }
        };
    }

    #endregion
}
