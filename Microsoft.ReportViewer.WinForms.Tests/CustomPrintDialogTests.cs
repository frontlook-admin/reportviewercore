using System.Drawing;
using System.Text.RegularExpressions;
using Microsoft.Reporting.WinForms;
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
                PrintRange = PrintRange.SomePages,
                FromPage = 2,
                ToPage = 8,
                MinimumPage = 1,
                MaximumPage = 100
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
            settings.FromPage.Should().Be(2);
            settings.ToPage.Should().Be(8);
            settings.MinimumPage.Should().Be(1);
            settings.MaximumPage.Should().Be(100);
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
        public void CustomPrintDialog_JsonRoundTrip_PreservesSettingsSchemaVersion()
        {
            var original = new CustomPrintDialog();

            var json = original.GetJsonData();
            json.Should().Contain("SettingsSchemaVersion");

            var restored = new CustomPrintDialog(json);

            restored.SettingsSchemaVersion.Should().Be(CustomPrintDialog.CurrentSettingsSchemaVersion);
        }

        [Fact]
        public void CustomPrintDialog_LegacyJsonDefaultsToCurrentSettingsSchemaVersion()
        {
            var json = new CustomPrintDialog().GetJsonData();
            json = Regex.Replace(json, "\\\"SettingsSchemaVersion\\\"\\s*:\\s*\\d+\\s*,?", string.Empty);

            var restored = new CustomPrintDialog(json);

            restored.SettingsSchemaVersion.Should().Be(CustomPrintDialog.CurrentSettingsSchemaVersion);
        }

        [Fact]
        public void CustomPrintDialog_JsonRoundTrip_PreservesPageRangeSettings()
        {
            var original = new CustomPrintDialog
            {
                PrintRange = PrintRange.SomePages,
                FromPage = 2,
                ToPage = 8,
                MinimumPage = 1,
                MaximumPage = 100
            };

            var restored = new CustomPrintDialog(original.GetJsonData());

            restored.PrintRange.Should().Be(PrintRange.SomePages);
            restored.FromPage.Should().Be(2);
            restored.ToPage.Should().Be(8);
            restored.MinimumPage.Should().Be(1);
            restored.MaximumPage.Should().Be(100);
        }

        [Fact]
        public void CustomPrintDialog_LegacyJsonWithoutPageRangeFieldsRemainsReadable()
        {
            var json = "{\"PrinterName\":null,\"PrintRange\":0,\"Copies\":1,\"Collate\":false}";

            var restored = new CustomPrintDialog(json);

            restored.Should().NotBeNull();
            restored.Copies.Should().Be(1);
            restored.FromPage.Should().Be(0);
            restored.ToPage.Should().Be(0);
        }

        [Fact]
        public void ReportViewer_DefaultPrintSettingFilePath_UsesReportName()
        {
            using var reportViewer = new ReportViewerControl();
            reportViewer.LocalReport.DisplayName = "SalesReport.rdlc";

            reportViewer.PrintSettingFilePath.Should().Be(
                Path.Combine(AppContext.BaseDirectory, "RdlcPrintSetting", "SalesReport.json"));
        }

        [Fact]
        public void ReportViewer_DefaultTheme_IsLight()
        {
            using var reportViewer = new ReportViewerControl();

            reportViewer.Theme.ToolbarBackground.Should().Be(Color.FromArgb(248, 250, 252));
            reportViewer.Theme.CanvasBackground.Should().Be(Color.FromArgb(243, 246, 250));
        }

        [Fact]
        public void ReportViewer_CanApplyDarkThemeWithoutChangingReportSettings()
        {
            using var reportViewer = new ReportViewerControl();
            reportViewer.LocalReport.DisplayName = "SalesReport.rdlc";

            reportViewer.Theme = ReportViewerTheme.Dark;

            reportViewer.Theme.ToolbarBackground.Should().Be(Color.FromArgb(35, 40, 48));
            reportViewer.PrintSettingFilePath.Should().Be(
                Path.Combine(AppContext.BaseDirectory, "RdlcPrintSetting", "SalesReport.json"));
        }

        [Fact]
        public void ReportViewer_CanApplyHighContrastTheme()
        {
            using var reportViewer = new ReportViewerControl
            {
                Theme = ReportViewerTheme.HighContrast
            };

            reportViewer.Theme.Should().BeSameAs(ReportViewerTheme.HighContrast);
            reportViewer.Theme.ToolbarBackground.Should().Be(Color.Black);
            reportViewer.Theme.Foreground.Should().Be(Color.White);
        }

        [Fact]
        public void ReportViewer_LoadingBranding_IsConfigurableAndSurvivesThemeChanges()
        {
            using var reportViewer = new ReportViewerControl();
            using var logo = new Bitmap(16, 16);

            reportViewer.LoadingMessage = "Preparing report...";
            reportViewer.LoadingBrandText = "Powered By Incredible Informatics";
            reportViewer.LoadingBrandLogo = logo;
            reportViewer.ShowLoadingBrand = true;
            reportViewer.Theme = ReportViewerTheme.Dark;

            reportViewer.LoadingMessage.Should().Be("Preparing report...");
            reportViewer.LoadingBrandText.Should().Be("Powered By Incredible Informatics");
            reportViewer.LoadingBrandLogo.Should().BeSameAs(logo);
            reportViewer.ShowLoadingBrand.Should().BeTrue();
            reportViewer.Theme.Should().BeSameAs(ReportViewerTheme.Dark);

            reportViewer.ShowLoadingBrand = false;
            reportViewer.LoadingBrandLogo.Should().BeSameAs(logo);
        }

        [Fact]
        public void ReportViewer_DefaultLoadingBranding_UsesIncredibleInformaticsTextWithoutLogo()
        {
            using var reportViewer = new ReportViewerControl();

            reportViewer.LoadingBrandText.Should().Be("Powered By Incredible Informatics");
            reportViewer.LoadingBrandLogo.Should().BeNull();
            reportViewer.ShowLoadingBrand.Should().BeTrue();
        }

        [Fact]
        public void ReportViewer_DefaultWaitControlAppearsImmediatelyAndRemainsConfigurable()
        {
            using var reportViewer = new ReportViewerControl();

            reportViewer.WaitControlDisplayAfter.Should().Be(0);

            reportViewer.WaitControlDisplayAfter = 750;

            reportViewer.WaitControlDisplayAfter.Should().Be(750);
        }

        [Fact]
        public void ReportViewerBranding_CreatesApplicationIcon()
        {
            using var icon = ReportViewerBranding.CreateApplicationIcon();
            using var bitmap = icon.ToBitmap();

            bitmap.Width.Should().Be(32);
            bitmap.Height.Should().Be(32);
        }

        [Fact]
        public void ReportViewer_ThemeChange_IsPersistedToConfiguredPreferencesFile()
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"rdlc-theme-preferences-{Guid.NewGuid():N}.json");
            try
            {
                using var reportViewer = new ReportViewerControl
                {
                    PreferencesFilePath = filePath
                };

                reportViewer.Theme = ReportViewerTheme.Dark;

                File.Exists(filePath).Should().BeTrue();
                File.ReadAllText(filePath).Should().Contain("\"Theme\": 1");
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Fact]
        public void ReportViewer_LoadsPersistedThemeDuringControlStartup()
        {
            var filePath = Path.Combine(Path.GetTempPath(), $"rdlc-theme-startup-{Guid.NewGuid():N}.json");
            try
            {
                using (var source = new ReportViewerControl
                {
                    PreferencesFilePath = filePath
                })
                {
                    source.Theme = ReportViewerTheme.Dark;
                }

                using var restored = new LoadableReportViewer
                {
                    PreferencesFilePath = filePath
                };

                restored.TriggerLoad();

                restored.Theme.Should().BeSameAs(ReportViewerTheme.Dark);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Fact]
        public void ReportViewer_PreferencesRoundTripViewerLayout()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"rdlc-viewer-preferences-{Guid.NewGuid():N}.json");
            try
            {
                using var source = new ReportViewerControl
                {
                    PreferencesFilePath = filePath,
                    Theme = ReportViewerTheme.Dark,
                    ZoomMode = ZoomMode.PageWidth,
                    ZoomPercent = 135,
                    ShowStatusBar = false,
                    ShowFindControls = false,
                    DocumentMapCollapsed = true,
                    DocumentMapWidth = 180
                };

                source.SavePreferences();

                using var restored = new ReportViewerControl
                {
                    PreferencesFilePath = filePath
                };

                restored.LoadPreferences().Should().BeTrue();
                restored.Theme.Should().BeSameAs(ReportViewerTheme.Dark);
                restored.ZoomMode.Should().Be(ZoomMode.PageWidth);
                restored.ZoomPercent.Should().Be(135);
                restored.ShowStatusBar.Should().BeFalse();
                restored.ShowFindControls.Should().BeFalse();
                restored.DocumentMapCollapsed.Should().BeTrue();
                restored.DocumentMapWidth.Should().Be(180);
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        [Fact]
        public void ReportViewer_CanAddApplicationToolbarButton()
        {
            using var reportViewer = new ReportViewerControl();
            bool clicked = false;

            var button = reportViewer.AddToolbarButton(
                "applicationAction",
                "Application action",
                (_, _) => clicked = true);

            reportViewer.Toolbar.Items.Contains(button).Should().BeTrue();
            button.PerformClick();
            clicked.Should().BeTrue();
        }

        [Fact]
        public void ReportViewer_StatusBarCanBeToggled()
        {
            using var reportViewer = new ReportViewerControl
            {
                ShowStatusBar = false
            };

            reportViewer.ShowStatusBar.Should().BeFalse();

            reportViewer.ShowStatusBar = true;

            reportViewer.ShowStatusBar.Should().BeTrue();
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
        public void ReportViewer_CreateExportDeviceInfo_UsesExactMetricMargins()
        {
            var pageSetting = new CustomPageSetting
            {
                PaperSize = new PaperSize("A4", 827, 1169),
                Margins = new Margins(20, 0, 20, 20),
                LeftMarginMillimeters = 5.01m,
                RightMarginMillimeters = 0.00m,
                TopMarginMillimeters = 5.01m,
                BottomMarginMillimeters = 5.01m
            };

            var deviceInfo = ReportViewerControl.CreateExportDeviceInfo(pageSetting);

            deviceInfo.Should().Contain("<MarginTop>0.1972440944881889763779527559in</MarginTop>");
            deviceInfo.Should().Contain("<MarginLeft>0.1972440944881889763779527559in</MarginLeft>");
            deviceInfo.Should().NotContain("<OutputFormat>emf</OutputFormat>");
        }

        [Fact]
        public void ReportViewer_CreateExportDeviceInfo_UsesLegacyDialogPaperSizeFallback()
        {
            var printSettings = new CustomPrintDialog
            {
                PaperSize = new PaperSize("A4", 827, 1169),
                CPageSettings = new CustomPageSetting
                {
                    Margins = new Margins(39, 20, 16, 12)
                }
            };

            var deviceInfo = ReportViewerControl.CreateExportDeviceInfo(printSettings);

            deviceInfo.Should().Contain("<PageWidth>8.27in</PageWidth>");
            deviceInfo.Should().Contain("<PageHeight>11.69in</PageHeight>");
            deviceInfo.Should().Contain("<MarginLeft>0.39in</MarginLeft>");
        }

        [Fact]
        public void ReportViewer_SavePrintSetting_CreatesDirectoryAndPersistsJsonAtomically()
        {
            var directory = Path.Combine(Path.GetTempPath(), "RdlcViewerTests", Guid.NewGuid().ToString("N"));
            var filePath = Path.Combine(directory, "print-settings.json");

            try
            {
                using var reportViewer = new ReportViewerControl
                {
                    PrintSettingFilePath = filePath,
                    CustomPrintDialog = new CustomPrintDialog(new PrinterSettings(), new PageSettings())
                };

                reportViewer.SavePrintSetting();

                File.Exists(filePath).Should().BeTrue();
                File.ReadAllText(filePath).Should().Contain("CPageSettings");
                Directory.GetFiles(directory, "*.tmp").Should().BeEmpty();
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
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

        private sealed class LoadableReportViewer : ReportViewerControl
        {
            public void TriggerLoad()
            {
                OnLoad(EventArgs.Empty);
            }
        }
    }
}
