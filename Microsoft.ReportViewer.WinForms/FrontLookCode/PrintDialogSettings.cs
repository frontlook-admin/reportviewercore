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

                if (!string.IsNullOrEmpty(PrinterName))
                {
                    pd.PrinterSettings.PrinterName = PrinterName;
                }

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
                        var paperSize = CPageSettings.PaperSize ?? new PaperSize("A4", 827, 1169);
                        var pageSettings = CPageSettings.GetPageSettings();
                        
                        if (!pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x == pageSettings.PaperSize))
                        {
                            pd.PrinterSettings.PaperSizes.Add(paperSize);
                        }
                        
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = pageSettings.PaperSize;
                        pd.PrinterSettings.DefaultPageSettings.Landscape = pageSettings.Landscape;
                    }
                }

                return pd;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PrintDialogSettings.CreatePrintDialog: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                
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

            if (!string.IsNullOrEmpty(PrinterName))
            {
                printDialog.PrinterSettings.PrinterName = PrinterName;
            }

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
                    var paperSize = CPageSettings.PaperSize ?? new PaperSize("A4", 827, 1169);
                    var pageSettings = CPageSettings.GetPageSettings();
                    
                    if (!printDialog.PrinterSettings.PaperSizes.Cast<PaperSize>().Any(x => x == pageSettings.PaperSize))
                    {
                        printDialog.PrinterSettings.PaperSizes.Add(paperSize);
                    }
                    
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSize = pageSettings.PaperSize;
                    printDialog.PrinterSettings.DefaultPageSettings.Landscape = pageSettings.Landscape;
                }
            }
        }
    }
}
