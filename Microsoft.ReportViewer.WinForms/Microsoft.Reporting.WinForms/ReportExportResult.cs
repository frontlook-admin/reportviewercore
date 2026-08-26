using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Reporting.WinForms
{
    public sealed class ReportExportSecondaryFile
    {
        internal ReportExportSecondaryFile(string name, string filePath, long bytesWritten, bool isInternal)
        {
            Name = name;
            FilePath = filePath;
            BytesWritten = bytesWritten;
            IsInternal = isInternal;
        }

        public string Name { get; }

        public string FilePath { get; }

        public long BytesWritten { get; }

        public bool IsInternal { get; }
    }

    public sealed class ReportExportResult
    {
        internal ReportExportResult(string format, string filePath, long bytesWritten, IEnumerable<ReportExportSecondaryFile> secondaryFiles = null)
        {
            Format = format;
            FilePath = filePath;
            BytesWritten = bytesWritten;
            SecondaryFiles = new ReadOnlyCollection<ReportExportSecondaryFile>(
                new List<ReportExportSecondaryFile>(secondaryFiles ?? Array.Empty<ReportExportSecondaryFile>()));
        }

        public string Format { get; }

        public string FilePath { get; }

        public long BytesWritten { get; }

        public IReadOnlyList<ReportExportSecondaryFile> SecondaryFiles { get; }

        public IReadOnlyList<ReportExportSecondaryFile> SecondaryStreams => SecondaryFiles;
    }
}
