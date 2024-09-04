using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrontLookCoreLibraryAssembly.FL_General;
using System.Windows.Forms;
using System.IO;
using Microsoft.Reporting.WinForms;
using System.Text.Json.Serialization;

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
        }

        public bool Color { get; set; }
        public bool Landscape { get; set; }
        //public bool? MetricEnabled { get; set; }
        public PaperSize PaperSize { get; set; }
        //private static double MarginConversion = 0.394;
        private static double MarginConversion = (1/2.54);
        public Margins Margins { get; set; }

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
                Margins = Margins,
                PaperSource = PaperSource,
                PrinterResolution = PrinterResolution
            };

        }

        public Margins GetSetupMargin()
        {
            try
            {
                var m = new Margins();
                var Top = ((double)Margins.Top) / MarginConversion;
                var Bottom = ((double)Margins.Bottom) / MarginConversion;
                var Left = ((double)Margins.Left) / MarginConversion;
                var Right = ((double)Margins.Right) / MarginConversion;

                m.Top = (int)Top;
                m.Bottom = (int)Bottom;
                m.Left = (int)Left;
                m.Right = (int)Right;

                return m;
            }
            catch { 
                return Margins; 
            }
        }

        public PageSettings GetSetupPageSettings()
        {
            try
            {
                return new PageSettings()
                {
                    Color = Color,
                    Landscape = Landscape,
                    PaperSize = PaperSize,
                    Margins = GetSetupMargin(),
                    PaperSource = PaperSource,
                    PrinterResolution = PrinterResolution
                };
            }
            catch
            {
                return new PageSettings()
                {
                    Color = Color,
                    Landscape = Landscape,
                    PaperSize = PaperSize,
                    Margins = Margins,
                    PaperSource = PaperSource,
                    PrinterResolution = PrinterResolution
                };
            }

        }
    }

    public class CustomPrintDialog
    {
        public CustomPrintDialog()
        {

        }

        public CustomPrintDialog(PrinterSettings PrinterSettings, PageSettings pageSettings, PrintType printType = PrintType.Mod)
        {
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
            var printDialog = JsonData.FL_CastToClass<CustomPrintDialog>();
            PrinterName = printDialog.PrinterName;
            AllowSomePages = printDialog.AllowSomePages;
            AllowSelection = printDialog.AllowSelection;
            AllowPrintToFile = printDialog.AllowPrintToFile;
            UseEXDialog = printDialog.UseEXDialog;
            ShowNetwork = printDialog.ShowNetwork;
            PrintRange = printDialog.PrintRange;
            Copies = printDialog.Copies;
            Collate = printDialog.Collate;
            PaperSize = printDialog.PaperSize;
            Landscape = printDialog.Landscape;
            PrintType = printDialog.PrintType;

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

        public virtual string GetJsonData()
        {
            return this.FL_CastToJson();
        }

        public virtual PrinterSettings GetPrinterSettings()
        {
            return GetPrintDialog().PrinterSettings;
        }
        public virtual PageSettings GetPageSettings()
        {
            GetPrintDialog();
            return CPageSettings.GetPageSettings();
        }
        public virtual PageSettings GetSetupPageSettings()
        {
            GetPrintDialog();
            return CPageSettings.GetSetupPageSettings();
        }

        public virtual PrintDialog GetPrintDialog()
        {
            var pd = new PrintDialog();
            try
            {
                pd.PrinterSettings.PrinterName = PrinterName;
                pd.AllowSomePages = AllowSomePages;
                pd.AllowSelection = AllowSelection;
                pd.AllowPrintToFile = AllowPrintToFile;
                pd.UseEXDialog = UseEXDialog;
                pd.ShowNetwork = ShowNetwork;
                pd.PrinterSettings.PrintRange = PrintRange;
                pd.PrinterSettings.Copies = (short)Copies;
                pd.PrinterSettings.Collate = Collate;
                if (PrintType == PrintType.Default)
                {
                    if (PaperSize == null)
                    {
                        PaperSize = new PaperSize("A4", 827, 1169);
                    }
                    //pd.PrinterSettings.PaperSizes.Add(PaperSize);
                    //check if papersize exists
                    if (pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Where(x => x.PaperName == PaperSize.PaperName).Count() == 0)
                    {
                        pd.PrinterSettings.PaperSizes.Add(PaperSize);
                    }
                    pd.PrinterSettings.DefaultPageSettings.PaperSize = PaperSize;
                    pd.PrinterSettings.DefaultPageSettings.Landscape = Landscape;

                    CPageSettings = new CustomPageSetting(pd.PrinterSettings.DefaultPageSettings);
                }
                else
                {
                    if(CPageSettings == null)
                    {

                        if (PaperSize == null)
                        {
                            PaperSize = new PaperSize("A4", 827, 1169);
                        }
                        //pd.PrinterSettings.PaperSizes.Add(PaperSize);
                        //check if papersize exists
                        if (pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Where(x => x.PaperName == PaperSize.PaperName).Count() == 0)
                        {
                            pd.PrinterSettings.PaperSizes.Add(PaperSize);
                        }
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = PaperSize;
                        pd.PrinterSettings.DefaultPageSettings.Landscape = Landscape;

                        CPageSettings = new CustomPageSetting(pd.PrinterSettings.DefaultPageSettings);
                    }
                    else
                    {

                        if (CPageSettings.PaperSize == null)
                        {
                            CPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
                            PaperSize = CPageSettings.PaperSize;
                        }

                        var pagest = CPageSettings.GetPageSettings();

                        //pd.PrinterSettings.PaperSizes.Add(PaperSize);
                        //check if papersize exists
                        if (pd.PrinterSettings.PaperSizes.Cast<PaperSize>().Where(x => x == pagest.PaperSize).Count() == 0)
                        {
                            pd.PrinterSettings.PaperSizes.Add(PaperSize);
                        }
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = pagest.PaperSize;
                        pd.PrinterSettings.DefaultPageSettings.Landscape = pagest.Landscape;

                        CPageSettings = new CustomPageSetting(pagest);
                    }
                }



                return pd;
            }
            catch (Exception ex)
            {
                var pf = new PrintDialog();

                pf.AllowSomePages = true;
                pf.AllowSelection = true;
                pf.ShowNetwork = true;
                pf.AllowPrintToFile = true;
                pf.UseEXDialog = false;
                return pf;
            }
        }
    }
}
