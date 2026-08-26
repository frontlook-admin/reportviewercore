using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Reporting.WinForms
{
    public sealed class ReportViewerRowSelection
    {
        public int PageNumber { get; }
        public int RowIndex { get; }
        public bool IsHeader { get; }
        public string Source { get; }
        public int SourceOrder { get; }
        public IReadOnlyList<string> Cells { get; }

        public ReportViewerRowSelection(int pageNumber, int rowIndex, bool isHeader, string source, int sourceOrder, IEnumerable<string> cells)
        {
            PageNumber = pageNumber;
            RowIndex = rowIndex;
            IsHeader = isHeader;
            Source = source ?? string.Empty;
            SourceOrder = sourceOrder;
            Cells = new ReadOnlyCollection<string>((cells ?? Enumerable.Empty<string>()).Select(cell => cell ?? string.Empty).ToList());
        }
    }

    public sealed class ReportViewerRowSelectionChangedEventArgs : EventArgs
    {
        public ReportViewerRowSelection Selection { get; }

        public ReportViewerRowSelectionChangedEventArgs(ReportViewerRowSelection selection)
        {
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }
    }
}
