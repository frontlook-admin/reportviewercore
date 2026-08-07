using System;

namespace Microsoft.Reporting.WinForms
{
    public sealed class ReportExportResult
    {
        internal ReportExportResult(string format, string filePath, long bytesWritten)
        {
            Format = format;
            FilePath = filePath;
            BytesWritten = bytesWritten;
        }

        public string Format { get; }

        public string FilePath { get; }

        public long BytesWritten { get; }
    }
}
