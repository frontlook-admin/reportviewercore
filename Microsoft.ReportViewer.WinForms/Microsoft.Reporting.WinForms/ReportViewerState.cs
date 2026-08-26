using System;

namespace Microsoft.Reporting.WinForms
{
    /// <summary>Immutable public snapshot of the viewer's presentation and render lifecycle.</summary>
    public sealed class ReportViewerState : IEquatable<ReportViewerState>
    {
        public ReportViewerState(
            int currentPage,
            int totalPages,
            PageCountMode pageCountMode,
            ZoomMode zoomMode,
            int zoomPercent,
            DisplayMode displayMode,
            bool isLoading,
            Exception error,
            bool isParameterDirty,
            long renderGeneration,
            UIState status = UIState.NoReport)
        {
            if (currentPage < 0) throw new ArgumentOutOfRangeException(nameof(currentPage));
            if (totalPages < 0) throw new ArgumentOutOfRangeException(nameof(totalPages));
            if (zoomPercent <= 0) throw new ArgumentOutOfRangeException(nameof(zoomPercent));
            if (renderGeneration < 0) throw new ArgumentOutOfRangeException(nameof(renderGeneration));
            CurrentPage = currentPage;
            TotalPages = totalPages;
            PageCountMode = pageCountMode;
            ZoomMode = zoomMode;
            ZoomPercent = zoomPercent;
            DisplayMode = displayMode;
            IsLoading = isLoading;
            Error = error;
            IsParameterDirty = isParameterDirty;
            RenderGeneration = renderGeneration;
            Status = status;
        }

        public int CurrentPage { get; }
        public int TotalPages { get; }
        public PageCountMode PageCountMode { get; }
        public ZoomMode ZoomMode { get; }
        public int ZoomPercent { get; }
        public DisplayMode DisplayMode { get; }
        public bool IsLoading { get; }
        public Exception Error { get; }
        public bool IsParameterDirty { get; }
        public long RenderGeneration { get; }
        public UIState Status { get; }

        public bool Equals(ReportViewerState other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return CurrentPage == other.CurrentPage && TotalPages == other.TotalPages
                && PageCountMode == other.PageCountMode && ZoomMode == other.ZoomMode
                && ZoomPercent == other.ZoomPercent && DisplayMode == other.DisplayMode
                && IsLoading == other.IsLoading && ReferenceEquals(Error, other.Error)
                && IsParameterDirty == other.IsParameterDirty && RenderGeneration == other.RenderGeneration
                && Status == other.Status;
        }

        public override bool Equals(object obj) => Equals(obj as ReportViewerState);
        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(CurrentPage, TotalPages, PageCountMode, ZoomMode, ZoomPercent),
            HashCode.Combine(DisplayMode, IsLoading, IsParameterDirty, RenderGeneration, Status));
    }

    public sealed class ReportViewerStateChangedEventArgs : EventArgs
    {
        public ReportViewerStateChangedEventArgs(ReportViewerState previous, ReportViewerState current)
        {
            Previous = previous ?? throw new ArgumentNullException(nameof(previous));
            Current = current ?? throw new ArgumentNullException(nameof(current));
        }

        public ReportViewerState Previous { get; }
        public ReportViewerState Current { get; }
    }
}
