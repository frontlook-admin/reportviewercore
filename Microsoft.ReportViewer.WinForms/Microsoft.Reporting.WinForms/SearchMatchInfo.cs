using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Microsoft.Reporting.WinForms
{
    [ComVisible(false)]
    public sealed class SearchMatchInfo
    {
        public string Text { get; }
        public int PageNumber { get; }
        public int MatchIndex { get; }
        public PointF Point { get; }

        public SearchMatchInfo(string text, int pageNumber, int matchIndex, PointF point)
        {
            Text = text ?? string.Empty;
            PageNumber = pageNumber;
            MatchIndex = matchIndex;
            Point = point;
        }
    }

    [ComVisible(false)]
    public sealed class SearchMatchChangedEventArgs : EventArgs
    {
        public SearchMatchInfo Match { get; }
        public int MatchCount { get; }

        public SearchMatchChangedEventArgs(SearchMatchInfo match, int matchCount)
        {
            Match = match;
            MatchCount = matchCount;
        }
    }
}
