using System.Drawing.Printing;

namespace Microsoft.Reporting.WinForms
{
    public sealed class ReportPrintCompletedEventArgs : System.EventArgs
    {
        internal ReportPrintCompletedEventArgs(PrinterSettings printerSettings, PageSettings pageSettings)
        {
            PrinterSettings = printerSettings;
            PageSettings = pageSettings;
        }

        public PrinterSettings PrinterSettings { get; }

        public PageSettings PageSettings { get; }
    }
}
