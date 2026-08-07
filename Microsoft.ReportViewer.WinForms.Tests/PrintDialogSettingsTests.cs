using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ReportViewer.WinForms.Tests
{
    /// <summary>
    /// Tests for PrintDialogSettings class to ensure proper resource management
    /// and settings transfer without memory leaks.
    /// </summary>
    public class PrintDialogSettingsTests
    {
        [Fact]
        public void Constructor_Default_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var settings = new PrintDialogSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.PrinterName.Should().BeNull();
            settings.Copies.Should().Be(0);
            settings.Collate.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithNullPrintDialog_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new PrintDialogSettings(null!);
            
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("printDialog");
        }

        [Fact]
        public void Constructor_WithValidPrintDialog_CopiesAllSettings()
        {
            // Arrange
            using var printDialog = new PrintDialog();
            printDialog.AllowSomePages = true;
            printDialog.AllowSelection = true;
            printDialog.AllowPrintToFile = true;
            printDialog.PrintToFile = false;
            printDialog.UseEXDialog = true;
            printDialog.ShowNetwork = false;
            printDialog.PrinterSettings.PrintRange = PrintRange.SomePages;
            printDialog.PrinterSettings.Copies = 3;
            printDialog.PrinterSettings.Collate = true;
            printDialog.PrinterSettings.FromPage = 1;
            printDialog.PrinterSettings.ToPage = 10;
            printDialog.PrinterSettings.MinimumPage = 1;
            printDialog.PrinterSettings.MaximumPage = 100;

            // Act
            var settings = new PrintDialogSettings(printDialog);

            // Assert
            settings.AllowSomePages.Should().BeTrue();
            settings.AllowSelection.Should().BeTrue();
            settings.AllowPrintToFile.Should().BeTrue();
            settings.PrintToFile.Should().BeFalse();
            settings.UseEXDialog.Should().BeTrue();
            settings.ShowNetwork.Should().BeFalse();
            settings.PrintRange.Should().Be(PrintRange.SomePages);
            settings.Copies.Should().Be(3);
            settings.Collate.Should().BeTrue();
            settings.FromPage.Should().Be(1);
            settings.ToPage.Should().Be(10);
            settings.MinimumPage.Should().Be(1);
            settings.MaximumPage.Should().Be(100);
        }

        [Fact]
        public void CreatePrintDialog_ReturnsNewConfiguredPrintDialog()
        {
            // Arrange
            var settings = new PrintDialogSettings
            {
                PrinterName = "TestPrinter",
                AllowSomePages = true,
                AllowSelection = true,
                Copies = 5,
                Collate = true,
                PrintRange = PrintRange.Selection
            };

            // Act
            using var printDialog = settings.CreatePrintDialog();

            // Assert
            printDialog.Should().NotBeNull();
            printDialog.AllowSomePages.Should().BeTrue();
            printDialog.AllowSelection.Should().BeTrue();
            printDialog.PrinterSettings.Copies.Should().Be(5);
            printDialog.PrinterSettings.Collate.Should().BeTrue();
            printDialog.PrinterSettings.PrintRange.Should().Be(PrintRange.Selection);
        }

        [Fact]
        public void CreatePrintDialog_CanBeDisposedMultipleTimes_NoException()
        {
            // Arrange
            var settings = new PrintDialogSettings();
            var printDialog = settings.CreatePrintDialog();

            // Act & Assert
            Action act = () =>
            {
                printDialog.Dispose();
                printDialog.Dispose(); // Second dispose should not throw
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void ApplyTo_WithNullPrintDialog_ThrowsArgumentNullException()
        {
            // Arrange
            var settings = new PrintDialogSettings();

            // Act & Assert
            Action act = () => settings.ApplyTo(null!);
            
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("printDialog");
        }

        [Fact]
        public void ApplyTo_WithValidPrintDialog_AppliesAllSettings()
        {
            // Arrange
            var settings = new PrintDialogSettings
            {
                AllowSomePages = true,
                AllowSelection = false,
                AllowPrintToFile = true,
                UseEXDialog = false,
                ShowNetwork = true,
                PrintRange = PrintRange.AllPages,
                Copies = 2,
                Collate = true,
                FromPage = 5,
                ToPage = 15
            };

            using var printDialog = new PrintDialog();

            // Act
            settings.ApplyTo(printDialog);

            // Assert
            printDialog.AllowSomePages.Should().BeTrue();
            printDialog.AllowSelection.Should().BeFalse();
            printDialog.AllowPrintToFile.Should().BeTrue();
            printDialog.UseEXDialog.Should().BeFalse();
            printDialog.ShowNetwork.Should().BeTrue();
            printDialog.PrinterSettings.PrintRange.Should().Be(PrintRange.AllPages);
            printDialog.PrinterSettings.Copies.Should().Be(2);
            printDialog.PrinterSettings.Collate.Should().BeTrue();
            printDialog.PrinterSettings.FromPage.Should().Be(5);
            printDialog.PrinterSettings.ToPage.Should().Be(15);
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var settings = new PrintDialogSettings();

            // Act
            settings.PrinterName = "MyPrinter";
            settings.Copies = 10;
            settings.Collate = true;
            settings.AllowSomePages = true;

            // Assert
            settings.PrinterName.Should().Be("MyPrinter");
            settings.Copies.Should().Be(10);
            settings.Collate.Should().BeTrue();
            settings.AllowSomePages.Should().BeTrue();
        }

        [Fact]
        public void RoundTrip_SettingsToPrintDialogAndBack_PreservesValues()
        {
            // Arrange
            var originalSettings = new PrintDialogSettings
            {
                PrinterName = "TestPrinter",
                AllowSomePages = true,
                Copies = 3,
                Collate = true,
                PrintRange = PrintRange.SomePages,
                FromPage = 2,
                ToPage = 8
            };

            // Act - Create PrintDialog from settings
            using var printDialog = originalSettings.CreatePrintDialog();
            
            // Create new settings from the PrintDialog
            var roundTripSettings = new PrintDialogSettings(printDialog);

            // Assert - All values should match
            roundTripSettings.AllowSomePages.Should().Be(originalSettings.AllowSomePages);
            roundTripSettings.Copies.Should().Be(originalSettings.Copies);
            roundTripSettings.Collate.Should().Be(originalSettings.Collate);
            roundTripSettings.PrintRange.Should().Be(originalSettings.PrintRange);
            roundTripSettings.FromPage.Should().Be(originalSettings.FromPage);
            roundTripSettings.ToPage.Should().Be(originalSettings.ToPage);
        }

        [Fact]
        public void CreatePrintDialog_WithEmptyPrinterName_DoesNotThrow()
        {
            // Arrange
            var settings = new PrintDialogSettings
            {
                PrinterName = string.Empty
            };

            // Act
            Action act = () =>
            {
                using var pd = settings.CreatePrintDialog();
            };

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void CreatePrintDialog_WithUnavailablePrinter_UsesDefaultPrinter()
        {
            var settings = new PrintDialogSettings
            {
                PrinterName = "RdlcViewerPrinterThatDoesNotExist"
            };

            using var printDialog = settings.CreatePrintDialog();

            printDialog.PrinterSettings.IsValid.Should().BeTrue();
        }

        [Fact]
        public void CreatePrintDialog_AppliesCustomMarginsToDefaultPageSettings()
        {
            var settings = new PrintDialogSettings
            {
                CPageSettings = new CustomPageSetting
                {
                    PaperSize = new PaperSize("A4", 827, 1169),
                    Margins = new Margins(20, 0, 40, 60)
                }
            };

            using var printDialog = settings.CreatePrintDialog();

            printDialog.PrinterSettings.DefaultPageSettings.Margins.Left.Should().Be(20);
            printDialog.PrinterSettings.DefaultPageSettings.Margins.Right.Should().Be(0);
            printDialog.PrinterSettings.DefaultPageSettings.Margins.Top.Should().Be(40);
            printDialog.PrinterSettings.DefaultPageSettings.Margins.Bottom.Should().Be(60);
        }

        [Fact]
        public void ApplyTo_WithEmptyPrinterName_DoesNotOverwritePrinterSettings()
        {
            // Arrange
            using var printDialog = new PrintDialog();
            var originalPrinterName = printDialog.PrinterSettings.PrinterName;
            
            var settings = new PrintDialogSettings
            {
                PrinterName = string.Empty
            };

            // Act
            settings.ApplyTo(printDialog);

            // Assert - Should not change the printer name when empty
            printDialog.PrinterSettings.PrinterName.Should().Be(originalPrinterName);
        }
    }
}
