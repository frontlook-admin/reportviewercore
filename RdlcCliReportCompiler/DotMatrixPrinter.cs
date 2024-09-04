using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;

namespace CliReportCompiler
{
    /*
    Implementation


    DotMatrixPrinter printer = new DotMatrixPrinter();
string printerName = "YourDotMatrixPrinterName"; // Replace with your printer name
string reportText = "Your report content here"; // Replace with your report content
printer.Print(printerName, reportText);

    */


    public class DotMatrixPrinter
    {
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, ref DOCINFOA pDocInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        public void Print(string printerName, string textToPrint)
        {
            IntPtr printerHandle;
            DOCINFOA docInfo = new DOCINFOA();
            docInfo.pDocName = "DotMatrixDocument";
            docInfo.pDataType = "RAW";

            // Open the printer.
            if (OpenPrinter(printerName, out printerHandle, IntPtr.Zero))
            {
                // Start a document and a page, then write the text to the printer.
                if (StartDocPrinter(printerHandle, 1, ref docInfo))
                {
                    if (StartPagePrinter(printerHandle))
                    {
                        // Convert the string to bytes and write.
                        IntPtr pBytes;
                        int dwCount = textToPrint.Length;
                        pBytes = Marshal.StringToCoTaskMemAnsi(textToPrint);
                        int dwWritten = 0;

                        WritePrinter(printerHandle, pBytes, dwCount, out dwWritten);

                        // End the page and document.
                        EndPagePrinter(printerHandle);
                        EndDocPrinter(printerHandle);

                        // Free the allocated memory.
                        Marshal.FreeCoTaskMem(pBytes);
                    }
                }
                // Close the printer.
                ClosePrinter(printerHandle);
            }
        }
    }
}
