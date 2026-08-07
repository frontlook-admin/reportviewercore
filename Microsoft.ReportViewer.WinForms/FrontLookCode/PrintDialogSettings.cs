using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace Microsoft.ReportViewer.WinForms.FrontLookCode
{
    /// <summary>
    /// Represents printer and page settings without holding undisposed resources.
    /// This class replaces the need to return PrintDialog objects directly.
    /// </summary>
    public class PrintDialogSettings
    {
        /// <summary>
        /// Initializes a new instance of the PrintDialogSettings class.
        /// </summary>
        public PrintDialogSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance from a PrintDialog object.
        /// </summary>
        /// <param name="printDialog">The PrintDialog to extract settings from.</param>
        public PrintDialogSettings(PrintDialog printDialog)
        {
            if (printDialog == null)
                throw new ArgumentNullException(nameof(printDialog));

            PrinterName = printDialog.PrinterSettings?.PrinterName;
            AllowSomePages = printDialog.AllowSomePages;
            AllowSelection = printDialog.AllowSelection;
            AllowPrintToFile = printDialog.AllowPrintToFile;
            PrintToFile = printDialog.PrintToFile;
            UseEXDialog = printDialog.UseEXDialog;
            ShowNetwork = printDialog.ShowNetwork;
            
            if (printDialog.PrinterSettings != null)
            {
                PrintRange = printDialog.PrinterSettings.PrintRange;
                Copies = printDialog.PrinterSettings.Copies;
                Collate = printDialog.PrinterSettings.Collate;
                FromPage = printDialog.PrinterSettings.FromPage;
                ToPage = printDialog.PrinterSettings.ToPage;
                MinimumPage = printDialog.PrinterSettings.MinimumPage;
                MaximumPage = printDialog.PrinterSettings.MaximumPage;
            }
        }

        /// <summary>
        /// Gets or sets the name of the printer.
        /// </summary>
        public string PrinterName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can specify a page range.
        /// </summary>
        public bool AllowSomePages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can select what to print.
        /// </summary>
        public bool AllowSelection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user can print to file.
        /// </summary>
        public bool AllowPrintToFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether printing to file is enabled.
        /// </summary>
        public bool PrintToFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the extended print dialog.
        /// </summary>
        public bool UseEXDialog { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the network printer selection is shown.
        /// </summary>
        public bool ShowNetwork { get; set; }

        /// <summary>
        /// Gets or sets the print range.
        /// </summary>
        public PrintRange PrintRange { get; set; }

        /// <summary>
        /// Gets or sets the number of copies to print.
        /// </summary>
        public short Copies { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether copies should be collated.
        /// </summary>
        public bool Collate { get; set; }

        /// <summary>
        /// Gets or sets the starting page number.
        /// </summary>
        public int FromPage { get; set; }

        /// <summary>
        /// Gets or sets the ending page number.
        /// </summary>
        public int ToPage { get; set; }

        /// <summary>
        /// Gets or sets the minimum page number that can be selected.
        /// </summary>
        public int MinimumPage { get; set; }

        /// <summary>
        /// Gets or sets the maximum page number that can be selected.
        /// </summary>
        public int MaximumPage { get; set; }

        /// <summary>
        /// Gets or sets the paper size for printing.
        /// </summary>
        public PaperSize PaperSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the page orientation is landscape.
        /// </summary>
        public bool Landscape { get; set; }

        /// <summary>
        /// Gets or sets the custom page settings.
        /// </summary>
        public CustomPageSetting CPageSettings { get; set; }

        /// <summary>
        /// Gets or sets the print type.
        /// </summary>
        public PrintType PrintType { get; set; }

        /// <summary>
        /// Creates a new PrintDialog with the settings from this instance.
        /// The caller is responsible for disposing the returned PrintDialog.
        /// </summary>
        /// <returns>A new PrintDialog configured with these settings.</returns>
        public PrintDialog CreatePrintDialog()
        {
            var pd = new PrintDialog();
            
            try
            {
                pd.AllowSomePages = AllowSomePages;
                pd.AllowSelection = AllowSelection;
                pd.AllowPrintToFile = AllowPrintToFile;
                pd.PrintToFile = PrintToFile;
                pd.UseEXDialog = UseEXDialog;
                pd.ShowNetwork = ShowNetwork;

                ApplyPrinterSettingsOrDefault(pd);

                pd.PrinterSettings.PrintRange = PrintRange;
                pd.PrinterSettings.Copies = Copies;
                pd.PrinterSettings.Collate = Collate;
                pd.PrinterSettings.FromPage = FromPage;
                pd.PrinterSettings.ToPage = ToPage;
                pd.PrinterSettings.MinimumPage = MinimumPage;
                pd.PrinterSettings.MaximumPage = MaximumPage;

                // Handle PaperSize and Landscape settings based on PrintType
                if (PrintType == PrintType.Default)
                {
                    var paperSize = PaperSize ?? new PaperSize("A4", 827, 1169);
                    
                    // Check if papersize exists in printer's supported sizes
                    if (!pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x.PaperName == paperSize.PaperName))
                    {
                        pd.PrinterSettings.PaperSizes.Add(paperSize);
                    }
                    
                    pd.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                    pd.PrinterSettings.DefaultPageSettings.Landscape = Landscape;
                }
                else // PrintType.Mod
                {
                    if (CPageSettings == null)
                    {
                        var paperSize = PaperSize ?? new PaperSize("A4", 827, 1169);
                        
                        if (!pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x.PaperName == paperSize.PaperName))
                        {
                            pd.PrinterSettings.PaperSizes.Add(paperSize);
                        }
                        
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                        pd.PrinterSettings.DefaultPageSettings.Landscape = Landscape;
                    }
                    else
                    {
                        var pageSettings = GetCustomPageSettings();
                        var paperSize = pageSettings?.PaperSize ?? PaperSize ?? new PaperSize("A4", 827, 1169);
                        
                        if (!pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x == paperSize))
                        {
                            pd.PrinterSettings.PaperSizes.Add(paperSize);
                        }
                        
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                        pd.PrinterSettings.DefaultPageSettings.Landscape = pageSettings?.Landscape ?? Landscape;
                    }
                }

                ApplyCustomPageSettings(pd.PrinterSettings);

                return pd;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrintDialogSettings.CreatePrintDialog: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                pd.Dispose();
                
                // Return a default PrintDialog if configuration fails
                var pf = new PrintDialog
                {
                    AllowSomePages = true,
                    AllowSelection = true,
                    ShowNetwork = true,
                    AllowPrintToFile = true,
                    UseEXDialog = false
                };
                
                return pf;
            }
        }

        /// <summary>
        /// Applies these settings to an existing PrintDialog.
        /// </summary>
        /// <param name="printDialog">The PrintDialog to configure.</param>
        public void ApplyTo(PrintDialog printDialog)
        {
            if (printDialog == null)
                throw new ArgumentNullException(nameof(printDialog));

            printDialog.AllowSomePages = AllowSomePages;
            printDialog.AllowSelection = AllowSelection;
            printDialog.AllowPrintToFile = AllowPrintToFile;
            printDialog.PrintToFile = PrintToFile;
            printDialog.UseEXDialog = UseEXDialog;
            printDialog.ShowNetwork = ShowNetwork;

            ApplyPrinterSettingsOrDefault(printDialog);

            printDialog.PrinterSettings.PrintRange = PrintRange;
            printDialog.PrinterSettings.Copies = Copies;
            printDialog.PrinterSettings.Collate = Collate;
            printDialog.PrinterSettings.FromPage = FromPage;
            printDialog.PrinterSettings.ToPage = ToPage;
            printDialog.PrinterSettings.MinimumPage = MinimumPage;
            printDialog.PrinterSettings.MaximumPage = MaximumPage;

            // Handle PaperSize and Landscape settings based on PrintType
            if (PrintType == PrintType.Default)
            {
                var paperSize = PaperSize ?? new PaperSize("A4", 827, 1169);
                
                // Check if papersize exists in printer's supported sizes
                if (!printDialog.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x.PaperName == paperSize.PaperName))
                {
                    printDialog.PrinterSettings.PaperSizes.Add(paperSize);
                }
                
                printDialog.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                printDialog.PrinterSettings.DefaultPageSettings.Landscape = Landscape;
            }
            else // PrintType.Mod
            {
                if (CPageSettings == null)
                {
                    var paperSize = PaperSize ?? new PaperSize("A4", 827, 1169);
                    
                    if (!printDialog.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x.PaperName == paperSize.PaperName))
                    {
                        printDialog.PrinterSettings.PaperSizes.Add(paperSize);
                    }
                    
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                    printDialog.PrinterSettings.DefaultPageSettings.Landscape = Landscape;
                }
                else
                {
                    var pageSettings = GetCustomPageSettings();
                    var paperSize = pageSettings?.PaperSize ?? PaperSize ?? new PaperSize("A4", 827, 1169);
                    
                    if (!printDialog.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x == paperSize))
                    {
                        printDialog.PrinterSettings.PaperSizes.Add(paperSize);
                    }
                    
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSize = paperSize;
                    printDialog.PrinterSettings.DefaultPageSettings.Landscape = pageSettings?.Landscape ?? Landscape;
                }
            }

            ApplyCustomPageSettings(printDialog.PrinterSettings);
        }

        private void ApplyCustomPageSettings(PrinterSettings printerSettings)
        {
            if (CPageSettings == null)
            {
                return;
            }

            var pageSettings = GetCustomPageSettings();
            if (pageSettings == null)
            {
                return;
            }

            var paperSize = pageSettings.PaperSize;
            if (paperSize != null && !printerSettings.PaperSizes.Cast<PaperSize>().Any(x =>
                x.PaperName == paperSize.PaperName && x.Width == paperSize.Width && x.Height == paperSize.Height))
            {
                printerSettings.PaperSizes.Add(paperSize);
            }

            var defaultPageSettings = printerSettings.DefaultPageSettings;
            defaultPageSettings.PaperSize = paperSize;
            defaultPageSettings.Landscape = pageSettings.Landscape;
            defaultPageSettings.Margins = pageSettings.Margins;
            defaultPageSettings.Color = pageSettings.Color;
            defaultPageSettings.PaperSource = pageSettings.PaperSource;
            defaultPageSettings.PrinterResolution = pageSettings.PrinterResolution;
        }

        private PageSettings GetCustomPageSettings()
        {
            if (CPageSettings == null)
            {
                return null;
            }

            var pageSetting = CPageSettings.Clone();
            pageSetting.PaperSize ??= PaperSize;
            return pageSetting.GetPageSettings();
        }

        private void ApplyPrinterSettingsOrDefault(PrintDialog printDialog)
        {
            if (string.IsNullOrWhiteSpace(PrinterName))
            {
                return;
            }

            try
            {
                printDialog.PrinterSettings.PrinterName = PrinterName;
                if (!printDialog.PrinterSettings.IsValid)
                {
                    Debug.WriteLine($"PrintDialogSettings: printer '{PrinterName}' is unavailable; using the default printer.");
                    printDialog.PrinterSettings = new PrinterSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrintDialogSettings: unable to select printer '{PrinterName}': {ex.GetType().Name} - {ex.Message}");
                printDialog.PrinterSettings = new PrinterSettings();
            }
        }
    }
}
