namespace Microsoft.ReportViewer.MAUI.Models;

/// <summary>
/// Represents a data source for a report.
/// </summary>
public class ReportDataSourceInfo
{
    /// <summary>
    /// Gets or sets the name of the data source.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data value.
    /// </summary>
    public object? Value { get; set; }

    public ReportDataSourceInfo()
    {
    }

    public ReportDataSourceInfo(string name, object? value)
    {
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Event arguments for report rendering completion.
/// </summary>
public class ReportRenderingCompleteEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether the rendering was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the total number of pages rendered.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Gets any error that occurred during rendering.
    /// </summary>
    public Exception? Error { get; }

    public ReportRenderingCompleteEventArgs(bool success, int totalPages, Exception? error = null)
    {
        Success = success;
        TotalPages = totalPages;
        Error = error;
    }
}

/// <summary>
/// Event arguments for report errors.
/// </summary>
public class ReportErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets or sets whether the error has been handled.
    /// </summary>
    public bool Handled { get; set; }

    public ReportErrorEventArgs(Exception exception)
    {
        Exception = exception;
    }
}

/// <summary>
/// Event arguments for report export requests.
/// </summary>
public class ReportExportEventArgs : EventArgs
{
    /// <summary>
    /// Gets the requested format.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets or sets whether the export has been handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets the exported file path (if handled externally).
    /// </summary>
    public string? ExportedFilePath { get; set; }

    public ReportExportEventArgs(string format)
    {
        Format = format;
    }
}

/// <summary>
/// Event arguments for report print requests.
/// </summary>
public class ReportPrintEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets whether the print request has been handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets the print settings.
    /// </summary>
    public PrintSettingsModel? PrintSettings { get; set; }
}

/// <summary>
/// Settings for report export operations.
/// </summary>
public class ReportExportSettings
{
    /// <summary>
    /// Gets or sets the export format.
    /// </summary>
    public string Format { get; set; } = "PDF";

    /// <summary>
    /// Gets or sets the output file path.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the device info XML for the renderer.
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Gets or sets whether to include hyperlinks in the export.
    /// </summary>
    public bool IncludeHyperlinks { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to auto-fit content.
    /// </summary>
    public bool AutoFit { get; set; } = true;
}

/// <summary>
/// Model for print settings (cross-platform compatible).
/// </summary>
public class PrintSettingsModel
{
    /// <summary>
    /// Gets or sets the printer name.
    /// </summary>
    public string? PrinterName { get; set; }

    /// <summary>
    /// Gets or sets the number of copies.
    /// </summary>
    public int Copies { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to collate copies.
    /// </summary>
    public bool Collate { get; set; }

    /// <summary>
    /// Gets or sets pages to print (e.g., "1-5,7,9-12").
    /// </summary>
    public string? PageRange { get; set; }

    /// <summary>
    /// Gets or sets whether to print landscape.
    /// </summary>
    public bool Landscape { get; set; }

    /// <summary>
    /// Gets or sets whether to print in color.
    /// </summary>
    public bool Color { get; set; } = true;

    /// <summary>
    /// Gets or sets the paper size name.
    /// </summary>
    public string? PaperSize { get; set; }

    /// <summary>
    /// Gets or sets duplex printing mode.
    /// </summary>
    public DuplexMode Duplex { get; set; } = DuplexMode.Simplex;
}

/// <summary>
/// Duplex printing modes.
/// </summary>
public enum DuplexMode
{
    /// <summary>
    /// Single-sided printing.
    /// </summary>
    Simplex,

    /// <summary>
    /// Double-sided, flip on long edge.
    /// </summary>
    Horizontal,

    /// <summary>
    /// Double-sided, flip on short edge.
    /// </summary>
    Vertical
}

/// <summary>
/// Report viewer display modes.
/// </summary>
public enum ReportDisplayMode
{
    /// <summary>
    /// Normal page view.
    /// </summary>
    Normal,

    /// <summary>
    /// Print preview mode.
    /// </summary>
    PrintLayout,

    /// <summary>
    /// Continuous scroll mode.
    /// </summary>
    Continuous
}

/// <summary>
/// Zoom modes for the report viewer.
/// </summary>
public enum ReportZoomMode
{
    /// <summary>
    /// Specific zoom percentage.
    /// </summary>
    Percent,

    /// <summary>
    /// Fit page width.
    /// </summary>
    PageWidth,

    /// <summary>
    /// Fit whole page.
    /// </summary>
    WholePage
}

/// <summary>
/// Event arguments for report export requests (with cancellation support).
/// </summary>
public class ReportExportRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the requested export format.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets or sets whether to cancel the export operation.
    /// </summary>
    public bool Cancel { get; set; }

    public ReportExportRequestedEventArgs(string format)
    {
        Format = format;
    }
}

/// <summary>
/// Event arguments for report print requests (with cancellation support).
/// </summary>
public class ReportPrintRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets whether to cancel the print operation.
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Gets or sets the number of copies to print.
    /// </summary>
    public int Copies { get; set; } = 1;

    /// <summary>
    /// Gets or sets the starting page (1-based).
    /// </summary>
    public int FromPage { get; set; } = 1;

    /// <summary>
    /// Gets or sets the ending page.
    /// </summary>
    public int ToPage { get; set; } = int.MaxValue;
}

/// <summary>
/// Event arguments for report page navigation.
/// </summary>
public class ReportNavigationEventArgs : EventArgs
{
    /// <summary>
    /// Gets the current page number.
    /// </summary>
    public int CurrentPage { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; }

    public ReportNavigationEventArgs(int currentPage, int totalPages)
    {
        CurrentPage = currentPage;
        TotalPages = totalPages;
    }
}

/// <summary>Represents a node in the report document map.</summary>
public sealed class ReportDocumentMapNode
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int? Page { get; init; }
    public IList<ReportDocumentMapNode> Children { get; init; } = new List<ReportDocumentMapNode>();
}

/// <summary>Search request raised by a viewer host.</summary>
public sealed class ReportSearchEventArgs : EventArgs
{
    public ReportSearchEventArgs(string query) => Query = query;
    public string Query { get; }
    public bool Cancel { get; set; }
    public int? MatchPage { get; set; }
}

/// <summary>Bookmark or document-map navigation request.</summary>
public sealed class ReportBookmarkEventArgs : EventArgs
{
    public ReportBookmarkEventArgs(string target) => Target = target;
    public string Target { get; }
    public bool Cancel { get; set; }
    public int? Page { get; set; }
}

/// <summary>Drillthrough navigation request.</summary>
public sealed class ReportDrillthroughEventArgs : EventArgs
{
    public ReportDrillthroughEventArgs(string reportName, IReadOnlyDictionary<string, string>? parameters = null)
    {
        ReportName = reportName;
        Parameters = parameters ?? new Dictionary<string, string>();
    }
    public string ReportName { get; }
    public IReadOnlyDictionary<string, string> Parameters { get; }
    public bool Cancel { get; set; }
}

/// <summary>Progress notification for an export operation.</summary>
public sealed class ReportExportProgressEventArgs : EventArgs
{
    public ReportExportProgressEventArgs(string format, double progress, bool isCompleted = false)
    {
        Format = format;
        Progress = Math.Clamp(progress, 0, 1);
        IsCompleted = isCompleted;
    }
    public string Format { get; }
    public double Progress { get; }
    public bool IsCompleted { get; }
    public bool IsCanceled { get; init; }
}

/// <summary>Accessibility and adaptive-theme settings for the viewer.</summary>
public sealed class ReportAccessibilitySettings
{
    public bool IsHighContrast { get; set; }
    public bool IsDarkTheme { get; set; }
    public bool ReduceMotion { get; set; }
    public string ViewerAutomationName { get; set; } = "Report viewer";
    public string StatusAutomationName { get; set; } = "Report status";
}

/// <summary>
/// Settings for configuring the ReportViewer control.
/// </summary>
public class ReportViewerSettings
{
    /// <summary>
    /// Gets or sets whether to show the toolbar.
    /// </summary>
    public bool ShowToolbar { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the export button.
    /// </summary>
    public bool ShowExportButton { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the print button.
    /// </summary>
    public bool ShowPrintButton { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show the refresh button.
    /// </summary>
    public bool ShowRefreshButton { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show zoom controls.
    /// </summary>
    public bool ShowZoomControls { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to show page navigator.
    /// </summary>
    public bool ShowPageNavigator { get; set; } = true;

    /// <summary>
    /// Gets or sets the zoom level percentage (default 100).
    /// </summary>
    public int ZoomLevel { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to auto-refresh the report.
    /// </summary>
    public bool AutoRefresh { get; set; }
}
