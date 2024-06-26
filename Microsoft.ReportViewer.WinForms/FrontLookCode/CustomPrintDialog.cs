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

namespace FrontLookCode
{

    public enum PrintType
    {
        Default = 1,
        Mod = 2
    }
    public class CustomPrintDialog
    {
        public CustomPrintDialog()
        {

        }

        public CustomPrintDialog(PrintDialog printDialog, PrintType printType = PrintType.Default)
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
            PaperSize = printDialog.PrinterSettings.DefaultPageSettings.PaperSize;
            Landscape = printDialog.PrinterSettings.DefaultPageSettings.Landscape;
        }

        public CustomPrintDialog(string JsonData)
        {
            var printDialog = JsonData.CastToClass<CustomPrintDialog>();
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

        public virtual string GetJsonData()
        {
            return this.CastToJson();
        }

        public virtual PrinterSettings GetPrintSettings()
        {
            if (this == null) return null;
            var ps = new PrinterSettings();
            try
            {
                ps.PrinterName = PrinterName;
                ps.Copies = (short)Copies;
                ps.Collate = Collate;
                if (PrintType == PrintType.Mod)
                {
                    if (PaperSize == null)
                    {
                        PaperSize = new PaperSize("A4", 827, 1169);
                    }
                    //ps.PaperSizes.Add(PaperSize);
                    //check if papersize exists
                    if (ps.PaperSizes.Cast<PaperSize>().Where(x => x.PaperName == PaperSize.PaperName).Count() == 0)
                    {
                        ps.PaperSizes.Add(PaperSize);
                    }
                    else
                    {
                        UpdatePageSettingsForPrinter(ps);
                    }
                }
                else
                {
                    ps.DefaultPageSettings.PaperSize = PaperSize;
                }
                return ps;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        internal void UpdatePageSettingsForPrinter(PrinterSettings printerSettings, PageSettings pageSettings)
        {
            if (!printerSettings.IsValid)
            {
                return;
            }
            int num = pageSettings.Landscape ? pageSettings.PaperSize.Height : pageSettings.PaperSize.Width;
            int num2 = pageSettings.Landscape ? pageSettings.PaperSize.Width : pageSettings.PaperSize.Height;
            foreach (PaperSize paperSize in printerSettings.PaperSizes)
            {
                if (num == paperSize.Width && num2 == paperSize.Height)
                {
                    pageSettings.Landscape = false;
                    pageSettings.PaperSize = paperSize;
                    break;
                }
                if (num == paperSize.Height && num2 == paperSize.Width)
                {
                    pageSettings.Landscape = true;
                    pageSettings.PaperSize = paperSize;
                    break;
                }
            }
            pageSettings.PrinterSettings = printerSettings;
        }

        internal void UpdatePageSettingsForPrinter(PrinterSettings printerSettings)
        {
            if (!printerSettings.IsValid)
            {
                return;
            }
            int num = Landscape ? PaperSize.Height : PaperSize.Width;
            int num2 = Landscape ? PaperSize.Width : PaperSize.Height;
            foreach (PaperSize paperSize in printerSettings.PaperSizes)
            {
                if (num == paperSize.Width && num2 == paperSize.Height)
                {
                    printerSettings.DefaultPageSettings.Landscape = false;
                    printerSettings.DefaultPageSettings.PaperSize = paperSize;
                    break;
                }
                if (num == paperSize.Height && num2 == paperSize.Width)
                {
                    printerSettings.DefaultPageSettings.Landscape = true;
                    printerSettings.DefaultPageSettings.PaperSize = paperSize;
                    break;
                }
            }
            //printerSettings = printerSettings;
        }

        public virtual PrintDialog GetPrintDialog()
        {
            if(this == null) return null;
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
                if (PrintType == PrintType.Mod)
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

    public static class CustomPrintDialogExt
    {
        public static PrinterSettings? GetSetPrinterSettingsJson(this LocalReport Rep, PrintType printType, string PrintSettingDirPath, string PrintSettingFilePath, bool useReportSize = false, bool PrintWithDialog = false)
        {
            using var pd = new PrintDialog();
            //pd.PrinterSettings.
            if (File.Exists(PrintSettingFilePath))
            {
                //var pf = File.ReadAllLines(PrintSettingFilePath);

#if DEBUG
                var _pf = File.ReadAllText(PrintSettingFilePath);
                var cpf = _pf.CastToClass<CustomPrintDialog>();
#else
                var cpf = File.ReadAllText(PrintSettingFilePath).CastToClass<CustomPrintDialog>();

#endif
                if (cpf != null)
                {
                    pd.AllowSomePages = cpf.AllowSomePages;
                    pd.AllowSelection = cpf.AllowSelection;
                    pd.AllowPrintToFile = cpf.AllowPrintToFile;
                    pd.UseEXDialog = cpf.UseEXDialog;
                    pd.ShowNetwork = cpf.ShowNetwork;
                    pd.PrinterSettings.PrintRange = cpf.PrintRange;
                    pd.PrinterSettings.PrinterName = cpf.PrinterName;
                    pd.PrinterSettings.Copies = cpf.Copies;
                    pd.PrinterSettings.PrintToFile = cpf.PrintToFile;
                    pd.PrinterSettings.Collate = cpf.Collate;
                    //var px = Rep.FL_GetPageSettings();
                    //px.Margins = new Margins(0, 0, 0, 0);

                    if (printType == PrintType.Default)
                    {
                        var px = Rep.GetDefaultPageSettings();
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = px.PaperSize;
                        pd.PrinterSettings.DefaultPageSettings.Margins = px.Margins;
                    }
                    else
                    {
                        pd.PrinterSettings.DefaultPageSettings.PaperSize = cpf.PaperSize;
                    }
                    pd.PrinterSettings.DefaultPageSettings.Landscape = cpf.Landscape;
                    if (PrintWithDialog)
                    {
                        pd.ShowDialog();
                        return pd.PrinterSettings;
                    }
                    return pd.PrinterSettings;
                }

                pd.AllowSomePages = true;
                pd.AllowSelection = true;
                pd.AllowPrintToFile = true;
                pd.UseEXDialog = true;
                pd.PrinterSettings.PrintRange = PrintRange.AllPages;
                if (pd.ShowDialog() != DialogResult.OK) return null;
                setUpPrinter(new CustomPrintDialog(pd, printType), PrintSettingDirPath, PrintSettingFilePath);
                return pd.PrinterSettings;
            }

            pd.AllowSomePages = true;
            pd.AllowSelection = true;
            pd.AllowPrintToFile = true;
            pd.ShowNetwork = true;
            pd.UseEXDialog = false;
            pd.PrinterSettings.PrintRange = PrintRange.AllPages;
            if (pd.ShowDialog() != DialogResult.OK) return null;
            setUpPrinter(new CustomPrintDialog(pd, printType), PrintSettingDirPath, PrintSettingFilePath);
            return pd.PrinterSettings;
        }

        private static void setUpPrinter(CustomPrintDialog pds, string PrintSettingDirPath, string PrintSettingFilePath)
        {
            if (!Directory.Exists(PrintSettingDirPath))
            {
                Directory.CreateDirectory(PrintSettingDirPath);
            }
            //if (File.Exists(PrintSettingFilePath)) File.Delete(PrintSettingFilePath);
            //File.Create(PrintSettingFilePath).Close();
            if (File.Exists(PrintSettingFilePath)) File.Delete(PrintSettingFilePath);
            File.Create(PrintSettingFilePath).Close();
            File.WriteAllText(PrintSettingFilePath, pds.CastToJson());
            //pds.ForEach(e => );
        }
    }
}
