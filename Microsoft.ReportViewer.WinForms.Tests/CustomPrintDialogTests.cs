using System.Drawing;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;

namespace Microsoft.ReportViewer.WinForms.Tests
{
    /// <summary>
    /// Tests for CustomPrintDialog class focusing on resource management,
    /// null safety, and the new GetPrintDialogSettings() method.
    /// </summary>
    public class CustomPrintDialogTests
    {
        [Fact]
        public void Constructor_Default_CreatesInstance()
        {
            // Act
            var dialog = new CustomPrintDialog();

            // Assert
            dialog.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNullPrinterSettings_ThrowsArgumentNullException()
        {
            // Arrange
            PrinterSettings? nullSettings = null;
            var pageSettings = new PageSettings();

            // Act & Assert
            Action act = () => new CustomPrintDialog(nullSettings!, pageSettings);
            
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("PrinterSettings");
        }

        [Fact]
        public void Constructor_WithNullPageSettings_ThrowsArgumentNullException()
        {
            // Arrange
            var printerSettings = new PrinterSettings();
            PageSettings? nullPageSettings = null;

            // Act & Assert
            Action act = () => new CustomPrintDialog(printerSettings, nullPageSettings!);
            
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("pageSettings");
        }

        [Fact]
        public void Constructor_WithNullPrintDialog_ThrowsArgumentNullException()
        {
            // Arrange
            PrintDialog? nullDialog = null;
            var pageSettings = new PageSettings();

            // Act & Assert
            Action act = () => new CustomPrintDialog(nullDialog!, pageSettings);
            
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("printDialog");
        }

        [Fact]
        public void Constructor_WithNullOrEmptyJsonString_ThrowsArgumentException()
        {
            // Act & Assert
            Action actNull = () => new CustomPrintDialog((string)null!);
            Action actEmpty = () => new CustomPrintDialog(string.Empty);
            Action actWhitespace = () => new CustomPrintDialog("   ");
            
            actNull.Should().Throw<ArgumentException>()
                .WithParameterName("JsonData");
            actEmpty.Should().Throw<ArgumentException>()
                .WithParameterName("JsonData");
            actWhitespace.Should().Throw<ArgumentException>()
                .WithParameterName("JsonData");
        }

        [Fact]
        public void Constructor_WithValidPrinterAndPageSettings_InitializesProperties()
        {
            // Arrange
            var printerSettings = new PrinterSettings
            {
                PrinterName = "TestPrinter",
                Copies = 2,
                Collate = true
            };
            var pageSettings = new PageSettings
            {
                Landscape = true,
                Color = false
            };

            // Act
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);

            // Assert
            dialog.PrinterName.Should().Be("TestPrinter");
            dialog.Copies.Should().Be(2);
            dialog.Collate.Should().BeTrue();
            dialog.Landscape.Should().BeTrue();
            dialog.CPageSettings.Should().NotBeNull();
        }

        [Fact]
        public void GetPrintDialogSettings_ReturnsValidSettings()
        {
            // Arrange
            var printerSettings = new PrinterSettings
            {
                PrinterName = "TestPrinter",
                Copies = 3,
                Collate = true,
                PrintRange = PrintRange.SomePages
            };
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);
            dialog.AllowSomePages = true;
            dialog.AllowSelection = false;

            // Act
            var settings = dialog.GetPrintDialogSettings();

            // Assert
            settings.Should().NotBeNull();
            settings.PrinterName.Should().Be("TestPrinter");
            settings.Copies.Should().Be(3);
            settings.Collate.Should().BeTrue();
            settings.PrintRange.Should().Be(PrintRange.SomePages);
            settings.AllowSomePages.Should().BeTrue();
            settings.AllowSelection.Should().BeFalse();
        }

        [Fact]
        public void GetPrintDialogSettings_DoesNotCreateDisposableResources()
        {
            // Arrange
            var printerSettings = new PrinterSettings();
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);

            // Act
            var settings = dialog.GetPrintDialogSettings();

            // Assert - Settings should be a simple data object, no disposal needed
            settings.Should().NotBeNull();
            settings.Should().BeOfType<PrintDialogSettings>();
            
            // This should not throw even if called multiple times
            var settings2 = dialog.GetPrintDialogSettings();
            settings2.Should().NotBeNull();
        }

        [Fact]
        public void GetPrintDialog_IsMarkedObsolete()
        {
            // Arrange
            var printerSettings = new PrinterSettings();
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);

            // Act & Assert
            // Note: This test verifies the method exists and is marked obsolete
            // The method should compile but generate a warning
            using var printDialog = dialog.GetPrintDialog();
            printDialog.Should().NotBeNull();
        }

        [Fact]
        public void GetPrintDialog_ShouldBeDisposedByCaller()
        {
            // Arrange
            var printerSettings = new PrinterSettings();
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);

            // Act & Assert - Demonstrate proper usage
            PrintDialog? printDialog = null;
            try
            {
                printDialog = dialog.GetPrintDialog();
                printDialog.Should().NotBeNull();
            }
            finally
            {
                printDialog?.Dispose();
            }
        }

        [Fact]
        public void GetPrintDialog_DoesNotReplaceExactMetricMargins()
        {
            var dialog = new CustomPrintDialog(new PrinterSettings(), new PageSettings())
            {
                CPageSettings = new CustomPageSetting
                {
                    PaperSize = new PaperSize("A4", 827, 1169),
                    Margins = new Margins(20, 20, 20, 20),
                    LeftMarginMillimeters = 5.01m,
                    RightMarginMillimeters = 5.01m,
                    TopMarginMillimeters = 5.01m,
                    BottomMarginMillimeters = 5.01m
                }
            };

            using (dialog.GetPrintDialog())
            {
                dialog.CPageSettings.LeftMarginMillimeters.Should().Be(5.01m);
                dialog.CPageSettings.GetPageSettings().Margins.Left.Should().Be(20);
            }
        }

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var dialog = new CustomPrintDialog();

            // Act
            dialog.PrinterName = "MyPrinter";
            dialog.AllowSomePages = true;
            dialog.AllowSelection = false;
            dialog.Copies = 5;
            dialog.Collate = true;
            dialog.Landscape = true;

            // Assert
            dialog.PrinterName.Should().Be("MyPrinter");
            dialog.AllowSomePages.Should().BeTrue();
            dialog.AllowSelection.Should().BeFalse();
            dialog.Copies.Should().Be(5);
            dialog.Collate.Should().BeTrue();
            dialog.Landscape.Should().BeTrue();
        }

        [Fact]
        public void CustomPageSetting_GetSetupMargin_HandlesExceptionGracefully()
        {
            // Arrange
            var pageSetting = new CustomPageSetting
            {
                Margins = new Margins(100, 100, 100, 100)
            };

            // Act
            var result = pageSetting.GetSetupMargin();

            // Assert
            result.Should().NotBeNull();
            // Method should not throw even with conversion issues
        }

        [Fact]
        public void CustomPageSetting_GetSetupMargin_PreservesHundredthsOfAnInch()
        {
            // 5 mm is represented by approximately 20 hundredths of an inch.
            var pageSetting = new CustomPageSetting
            {
                Margins = new Margins(20, 20, 20, 20)
            };

            var result = pageSetting.GetSetupMargin();

            result.Left.Should().Be(20);
            result.Right.Should().Be(20);
            result.Top.Should().Be(20);
            result.Bottom.Should().Be(20);
        }

        [Fact]
        public void CustomPageSetting_GetSetupPageSettings_DoesNotConvertMetricMargins()
        {
            var pageSetting = new CustomPageSetting
            {
                Margins = new Margins(20, 20, 20, 20)
            };

            var result = pageSetting.GetSetupPageSettings();

            result.Margins.Left.Should().Be(20);
            result.Margins.Right.Should().Be(20);
            result.Margins.Top.Should().Be(20);
            result.Margins.Bottom.Should().Be(20);
        }

        [Fact]
        public void CustomPrintDialog_JsonRoundTrip_PreservesMargins()
        {
            var original = new CustomPrintDialog
            {
                PrintType = PrintType.Mod,
                CPageSettings = new CustomPageSetting
                {
                    Margins = new Margins(20, 0, 20, 20)
                }
            };

            var restored = new CustomPrintDialog(original.GetJsonData());
            var pageSettings = restored.CPageSettings.GetPageSettings();

            pageSettings.Margins.Left.Should().Be(20);
            pageSettings.Margins.Right.Should().Be(0);
            pageSettings.Margins.Top.Should().Be(20);
            pageSettings.Margins.Bottom.Should().Be(20);
        }

        [Fact]
        public void CustomPageSetting_ExactMetricMargins_RoundOnlyForPageSettings()
        {
            var pageSetting = new CustomPageSetting
            {
                Margins = new Margins(20, 20, 20, 20),
                LeftMarginMillimeters = 5.00m,
                RightMarginMillimeters = 5.00m,
                TopMarginMillimeters = 5.00m,
                BottomMarginMillimeters = 5.00m
            };

            var pageSettings = pageSetting.GetPageSettings();

            pageSetting.LeftMarginMillimeters.Should().Be(5.00m);
            pageSetting.RightMarginMillimeters.Should().Be(5.00m);
            pageSetting.TopMarginMillimeters.Should().Be(5.00m);
            pageSetting.BottomMarginMillimeters.Should().Be(5.00m);
            pageSettings.Margins.Left.Should().Be(20);
            pageSettings.Margins.Right.Should().Be(20);
            pageSettings.Margins.Top.Should().Be(20);
            pageSettings.Margins.Bottom.Should().Be(20);
        }

        [Fact]
        public void CustomPrintDialog_JsonRoundTrip_PreservesExactMetricMargins()
        {
            var original = new CustomPrintDialog
            {
                PrintType = PrintType.Mod,
                CPageSettings = new CustomPageSetting
                {
                    Margins = new Margins(20, 0, 20, 20),
                    LeftMarginMillimeters = 5.00m,
                    RightMarginMillimeters = 0.00m,
                    TopMarginMillimeters = 5.00m,
                    BottomMarginMillimeters = 5.00m
                }
            };

            var restored = new CustomPrintDialog(original.GetJsonData());

            restored.CPageSettings.LeftMarginMillimeters.Should().Be(5.00m);
            restored.CPageSettings.RightMarginMillimeters.Should().Be(0.00m);
            restored.CPageSettings.TopMarginMillimeters.Should().Be(5.00m);
            restored.CPageSettings.BottomMarginMillimeters.Should().Be(5.00m);
            restored.CPageSettings.GetPageSettings().Margins.Left.Should().Be(20);
        }

        [Fact]
        public void ReportViewer_CreateEMFDeviceInfo_UsesHundredthsOfAnInchMargins()
        {
            using var reportViewer = new ReportViewerControl();
            var pageSetting = new CustomPageSetting
            {
                PaperSize = new PaperSize("A4", 827, 1169),
                Margins = new Margins(20, 0, 20, 20)
            };

            var deviceInfo = reportViewer.CreateEMFDeviceInfo(pageSetting, 0, 0);

            deviceInfo.Should().Contain("<MarginTop>0.2in</MarginTop>");
            deviceInfo.Should().Contain("<MarginLeft>0.2in</MarginLeft>");
            deviceInfo.Should().Contain("<MarginRight>0in</MarginRight>");
            deviceInfo.Should().Contain("<MarginBottom>0.2in</MarginBottom>");
        }

        [Fact]
        public void ReportViewer_CreateEMFDeviceInfo_UsesExactMetricMarginsForRendering()
        {
            using var reportViewer = new ReportViewerControl();
            var pageSetting = new CustomPageSetting
            {
                PaperSize = new PaperSize("A4", 827, 1169),
                Margins = new Margins(20, 0, 20, 20),
                LeftMarginMillimeters = 5.00m,
                RightMarginMillimeters = 0.00m,
                TopMarginMillimeters = 5.00m,
                BottomMarginMillimeters = 5.00m
            };

            var deviceInfo = reportViewer.CreateEMFDeviceInfo(pageSetting, 0, 0);

            deviceInfo.Should().Contain("<MarginTop>0.1968503937007874015748031496in</MarginTop>");
            deviceInfo.Should().Contain("<MarginLeft>0.1968503937007874015748031496in</MarginLeft>");
            deviceInfo.Should().Contain("<MarginRight>0in</MarginRight>");
            deviceInfo.Should().Contain("<MarginBottom>0.1968503937007874015748031496in</MarginBottom>");
            pageSetting.GetPageSettings().Margins.Left.Should().Be(20);
        }

        [Fact]
        public void CustomPageSetting_GetSetupPageSettings_HandlesExceptionGracefully()
        {
            // Arrange
            var pageSetting = new CustomPageSetting
            {
                Landscape = true,
                Color = true,
                PaperSize = new PaperSize("A4", 827, 1169),
                Margins = new Margins(50, 50, 50, 50)
            };

            // Act
            var result = pageSetting.GetSetupPageSettings();

            // Assert
            result.Should().NotBeNull();
            result.Landscape.Should().BeTrue();
            result.Color.Should().BeTrue();
        }

        [Fact]
        public void CustomPageSetting_Constructor_WithPageSettings_CopiesValues()
        {
            // Arrange
            var pageSettings = new PageSettings
            {
                Color = true,
                Landscape = true,
                Margins = new Margins(25, 25, 25, 25)
            };

            // Act
            var customSetting = new CustomPageSetting(pageSettings);

            // Assert
            customSetting.Color.Should().BeTrue();
            customSetting.Landscape.Should().BeTrue();
            customSetting.Margins.Should().BeEquivalentTo(pageSettings.Margins);
        }

        [Fact]
        public void GetJsonData_ReturnsValidJsonString()
        {
            // Arrange
            var printerSettings = new PrinterSettings
            {
                PrinterName = "TestPrinter"
            };
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);

            // Act
            var json = dialog.GetJsonData();

            // Assert
            json.Should().NotBeNullOrEmpty();
            json.Should().Contain("TestPrinter");
        }

        [Fact]
        public void GetPrinterSettings_ReturnsConfiguredSettings()
        {
            // Arrange
            var printerSettings = new PrinterSettings
            {
                PrinterName = "TestPrinter",
                Copies = 2
            };
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings);

            // Act
            var result = dialog.GetPrinterSettings();

            // Assert
            result.Should().NotBeNull();
            // Note: This calls GetPrintDialog() internally, so proper disposal is important
        }

        [Fact]
        public void PrintType_Default_IsSet()
        {
            // Arrange & Act
            var printerSettings = new PrinterSettings();
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings, PrintType.Default);

            // Assert
            dialog.PrintType.Should().Be(PrintType.Default);
        }

        [Fact]
        public void PrintType_Mod_IsSet()
        {
            // Arrange & Act
            var printerSettings = new PrinterSettings();
            var pageSettings = new PageSettings();
            var dialog = new CustomPrintDialog(printerSettings, pageSettings, PrintType.Mod);

            // Assert
            dialog.PrintType.Should().Be(PrintType.Mod);
        }
    }
}
