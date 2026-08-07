using System;
using System.Drawing.Printing;
using FrontLookCoreLibraryAssembly.FL_General;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using Microsoft.ReportViewer.Common.FrontLookCode;

namespace Microsoft.ReportViewer.WinForms.FrontLookCode
{

    public enum PrintType
    {
        Default = 1,
        Mod = 2
    }
    public class CustomPageSetting
    {
        public CustomPageSetting()
        {

        }

        public CustomPageSetting(PageSettings pageSettings)
        {
            Color = pageSettings.Color;
            Landscape = pageSettings.Landscape;
            Margins = pageSettings.Margins;
            PaperSource = pageSettings.PaperSource;
            PrinterResolution = pageSettings.PrinterResolution;
            PaperSize = pageSettings.PaperSize;
            SetMetricMarginsFromPageSettings(pageSettings);
        }

        public bool Color { get; set; }
        public bool Landscape { get; set; }
        public PaperSize PaperSize { get; set; }
        // System.Drawing.Printing.Margins stores all values in hundredths of an inch.
        public Margins Margins { get; set; }

        // Preserve exact metric values entered by the user. PageSettings.Margins
        // remains the rounded hundredths-of-an-inch representation used by GDI.
        public decimal? LeftMarginMillimeters { get; set; }
        public decimal? RightMarginMillimeters { get; set; }
        public decimal? TopMarginMillimeters { get; set; }
        public decimal? BottomMarginMillimeters { get; set; }

        //[JsonIgnore]
        //public Margins ReportMargins => GetSetupMargin();
        public PaperSource PaperSource { get; set; }
        public PrinterResolution PrinterResolution { get; set; }

        //[JsonIgnore]
        //public PageSettings ReportPageSettings => GetSetupPageSettings();

        public PageSettings GetPageSettings()
        {

            return new PageSettings()
            {
                Color = Color,
                Landscape = Landscape,
                PaperSize = PaperSize,
                Margins = GetPageMargins(),
                PaperSource = PaperSource,
                PrinterResolution = PrinterResolution
            };

        }

        public Margins GetSetupMargin()
        {
            return GetPageMargins();
        }

        public PageSettings GetSetupPageSettings()
        {
            // Kept as a compatibility entry point. PageSettings.Margins are always
            // hundredths of an inch, regardless of the PageSetupDialog display unit.
            return GetPageSettings();
        }

        public CustomPageSetting Clone()
        {
            return new CustomPageSetting
            {
                Color = Color,
                Landscape = Landscape,
                PaperSize = PaperSize,
                Margins = Margins == null ? null : new Margins(Margins.Left, Margins.Right, Margins.Top, Margins.Bottom),
                LeftMarginMillimeters = LeftMarginMillimeters,
                RightMarginMillimeters = RightMarginMillimeters,
                TopMarginMillimeters = TopMarginMillimeters,
                BottomMarginMillimeters = BottomMarginMillimeters,
                PaperSource = PaperSource,
                PrinterResolution = PrinterResolution
            };
        }

        internal void SetMetricMarginsFromPageSettings(PageSettings pageSettings)
        {
            if (pageSettings?.Margins == null)
            {
                return;
            }

            LeftMarginMillimeters = HundredthsOfAnInchToMillimeters(pageSettings.Margins.Left);
            RightMarginMillimeters = HundredthsOfAnInchToMillimeters(pageSettings.Margins.Right);
            TopMarginMillimeters = HundredthsOfAnInchToMillimeters(pageSettings.Margins.Top);
            BottomMarginMillimeters = HundredthsOfAnInchToMillimeters(pageSettings.Margins.Bottom);
        }

        internal void SetMetricMargins(decimal left, decimal right, decimal top, decimal bottom)
        {
            LeftMarginMillimeters = ValidateMetricMargin(left, nameof(left));
            RightMarginMillimeters = ValidateMetricMargin(right, nameof(right));
            TopMarginMillimeters = ValidateMetricMargin(top, nameof(top));
            BottomMarginMillimeters = ValidateMetricMargin(bottom, nameof(bottom));
        }

        internal void GetMetricMargins(out decimal left, out decimal right, out decimal top, out decimal bottom)
        {
            var sourceMargins = Margins ?? new Margins();
            left = LeftMarginMillimeters ?? HundredthsOfAnInchToMillimeters(sourceMargins.Left);
            right = RightMarginMillimeters ?? HundredthsOfAnInchToMillimeters(sourceMargins.Right);
            top = TopMarginMillimeters ?? HundredthsOfAnInchToMillimeters(sourceMargins.Top);
            bottom = BottomMarginMillimeters ?? HundredthsOfAnInchToMillimeters(sourceMargins.Bottom);
        }

        private Margins GetPageMargins()
        {
            GetMetricMargins(out var left, out var right, out var top, out var bottom);
            return new Margins(
                MillimetersToHundredthsOfAnInch(left),
                MillimetersToHundredthsOfAnInch(right),
                MillimetersToHundredthsOfAnInch(top),
                MillimetersToHundredthsOfAnInch(bottom));
        }

        private static decimal ValidateMetricMargin(decimal value, string parameterName)
        {
            if (value < 0m)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Margins cannot be negative.");
            }

            return value;
        }

        private static decimal HundredthsOfAnInchToMillimeters(int value)
        {
            return value * 25.4m / 100m;
        }

        private static int MillimetersToHundredthsOfAnInch(decimal value)
        {
            if (value < 0m)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Margins cannot be negative.");
            }

            var hundredthsOfAnInch = value / 25.4m * 100m;
            if (hundredthsOfAnInch > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Margin is too large.");
            }

            return checked((int)Math.Round(hundredthsOfAnInch, 0, MidpointRounding.AwayFromZero));
        }
    }

    public class CustomPrintDialog
    {
        public const int CurrentSettingsSchemaVersion = 2;

        public CustomPrintDialog()
        {

        }

        public CustomPrintDialog(PrinterSettings PrinterSettings, PageSettings pageSettings, PrintType printType = PrintType.Mod)
        {
            if (PrinterSettings == null)
                throw new ArgumentNullException(nameof(PrinterSettings));
            if (pageSettings == null)
                throw new ArgumentNullException(nameof(pageSettings));

            PrinterName = PrinterSettings.PrinterName;
            Copies = PrinterSettings.Copies;
            Collate = PrinterSettings.Collate;
            AllowSomePages = AllowSomePages;
            AllowSelection = AllowSelection;
            AllowPrintToFile = AllowPrintToFile;
            UseEXDialog = UseEXDialog;
            ShowNetwork = ShowNetwork;
            PrintRange = PrinterSettings.PrintRange;
            Copies = PrinterSettings.Copies;
            Collate = PrinterSettings.Collate;
            PrintType = printType;
            if (printType == PrintType.Default)
            {
                PaperSize = PrinterSettings.DefaultPageSettings.PaperSize;
                Landscape = PrinterSettings.DefaultPageSettings.Landscape;
            }
            else
            {
                PaperSize = pageSettings.PaperSize;
                Landscape = pageSettings.Landscape;
            }

            CPageSettings = new CustomPageSetting(pageSettings);
        }
        public CustomPrintDialog(PrintDialog printDialog, PageSettings pageSettings, PrintType printType = PrintType.Mod)
        {
            if (printDialog == null)
                throw new ArgumentNullException(nameof(printDialog));
            if (pageSettings == null)
                throw new ArgumentNullException(nameof(pageSettings));

            PrinterName = printDialog.PrinterSettings.PrinterName;
            Copies = printDialog.PrinterSettings.Copies;
            Collate = printDialog.PrinterSettings.Collate;
            AllowSomePages = printDialog.AllowSomePages;
            AllowSelection = printDialog.AllowSelection;
            AllowPrintToFile = printDialog.AllowPrintToFile;
            UseEXDialog = printDialog.UseEXDialog;
            ShowNetwork = printDialog.ShowNetwork;
            PrintRange = printDialog.PrinterSettings.PrintRange;
            Copies = printDialog.PrinterSettings.Copies;
            Collate = printDialog.PrinterSettings.Collate;
            PrintType = printType;
            if (printType == PrintType.Default)
            {
                PaperSize = printDialog.PrinterSettings.DefaultPageSettings.PaperSize;
                Landscape = printDialog.PrinterSettings.DefaultPageSettings.Landscape;
            }
            else
            {
                PaperSize = pageSettings.PaperSize;
                Landscape = pageSettings.Landscape;
            }

            CPageSettings = new CustomPageSetting(pageSettings);
        }

        public CustomPrintDialog(PrintDialog printDialog, PrintType printType = PrintType.Mod)
        {
            if (printDialog == null)
                throw new ArgumentNullException(nameof(printDialog));

            PrinterName = printDialog.PrinterSettings.PrinterName;
            Copies = printDialog.PrinterSettings.Copies;
            Collate = printDialog.PrinterSettings.Collate;
            AllowSomePages = printDialog.AllowSomePages;
            AllowSelection = printDialog.AllowSelection;
            AllowPrintToFile = printDialog.AllowPrintToFile;
            UseEXDialog = printDialog.UseEXDialog;
            ShowNetwork = printDialog.ShowNetwork;
            PrintRange = printDialog.PrinterSettings.PrintRange;
            Copies = printDialog.PrinterSettings.Copies;
            Collate = printDialog.PrinterSettings.Collate;
            PrintType = printType;
            if (printType == PrintType.Mod)
            {
                PaperSize = printDialog.PrinterSettings.DefaultPageSettings.PaperSize;
                Landscape = printDialog.PrinterSettings.DefaultPageSettings.Landscape;
            }

            CPageSettings = new CustomPageSetting(printDialog.PrinterSettings.DefaultPageSettings);
        }

        public CustomPrintDialog(string JsonData)
        {
            if (string.IsNullOrWhiteSpace(JsonData))
                throw new ArgumentException("JSON data cannot be null or empty.", nameof(JsonData));

            var printDialog = JsonData.CastToClass<CustomPrintDialog>();
            if (printDialog == null)
                throw new ArgumentException("JSON data does not contain valid print settings.", nameof(JsonData));

            PrinterName = printDialog.PrinterName;
            AllowSomePages = printDialog.AllowSomePages;
            AllowSelection = printDialog.AllowSelection;
            AllowPrintToFile = printDialog.AllowPrintToFile;
            PrintToFile = printDialog.PrintToFile;
            UseEXDialog = printDialog.UseEXDialog;
            ShowNetwork = printDialog.ShowNetwork;
            PrintRange = printDialog.PrintRange;
            Copies = printDialog.Copies;
            Collate = printDialog.Collate;
            PaperSize = printDialog.PaperSize;
            Landscape = printDialog.Landscape;
            PrintType = printDialog.PrintType;
            SettingsSchemaVersion = printDialog.SettingsSchemaVersion > 0
                ? printDialog.SettingsSchemaVersion
                : CurrentSettingsSchemaVersion;

            CPageSettings = printDialog.CPageSettings;

        }

        public virtual string PrinterName { get; set; }
        public virtual bool AllowSomePages { get; set; }
        public virtual bool AllowSelection { get; set; }
        public virtual bool AllowPrintToFile { get; set; }
        public virtual bool PrintToFile { get; set; }
        public virtual bool UseEXDialog { get; set; }
        public virtual bool ShowNetwork { get; set; }
        public virtual PrintRange PrintRange { get; set; }
        public virtual short Copies { get; set; }
        public virtual bool Collate { get; set; }
        public virtual PaperSize PaperSize { get; set; }
        public virtual bool Landscape { get; set; }
        public virtual PrintType PrintType { get; set; }
        public virtual CustomPageSetting CPageSettings { get; set; }
        public virtual int SettingsSchemaVersion { get; set; } = CurrentSettingsSchemaVersion;

        public virtual string GetJsonData()
        {
            return this.CastToJson();
        }

        public virtual PrinterSettings GetPrinterSettings()
        {
            using var printDialog = GetPrintDialogSettings().CreatePrintDialog();
            return printDialog.PrinterSettings;
        }
        public virtual PageSettings GetPageSettings()
        {
            var pageSetting = CPageSettings?.Clone() ?? new CustomPageSetting
            {
                PaperSize = PaperSize,
                Landscape = Landscape,
                Margins = new Margins()
            };

            // Older settings files stored PaperSize at the dialog level and
            // left CPageSettings.PaperSize null. Preserve that format when
            // reconstructing the effective report page settings.
            pageSetting.PaperSize ??= PaperSize;
            return pageSetting.GetPageSettings();
        }
        public virtual PageSettings GetSetupPageSettings()
        {
            return GetPageSettings();
        }

        /// <summary>
        /// Gets print dialog settings without creating undisposed resources.
        /// This is the recommended method for retrieving print settings.
        /// </summary>
        /// <returns>A PrintDialogSettings object containing all print configuration.</returns>
        public virtual PrintDialogSettings GetPrintDialogSettings()
        {
            var settings = new PrintDialogSettings
            {
                PrinterName = PrinterName,
                AllowSomePages = AllowSomePages,
                AllowSelection = AllowSelection,
                AllowPrintToFile = AllowPrintToFile,
                PrintToFile = PrintToFile,
                UseEXDialog = UseEXDialog,
                ShowNetwork = ShowNetwork,
                PrintRange = PrintRange,
                Copies = Copies,
                Collate = Collate,
                PaperSize = PaperSize,
                Landscape = Landscape,
                CPageSettings = CPageSettings,
                PrintType = PrintType
            };
            return settings;
        }

        /// <summary>
        /// Gets a PrintDialog object configured with current settings.
        /// </summary>
        /// <returns>A configured PrintDialog. The caller is responsible for disposing this object.</returns>
        [Obsolete("Use GetPrintDialogSettings() instead. This method creates undisposed resources.", false)]
        public virtual PrintDialog GetPrintDialog()
        {
            return GetPrintDialogSettings().CreatePrintDialog();
        }
    }
}
