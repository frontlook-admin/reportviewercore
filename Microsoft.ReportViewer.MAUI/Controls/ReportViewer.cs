using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Reporting.NETCore;
using Microsoft.ReportViewer.MAUI.Services;
// Aliased to avoid ambiguity with Microsoft.Reporting.NETCore types
using MauiModels = Microsoft.ReportViewer.MAUI.Models;

namespace Microsoft.ReportViewer.MAUI.Controls;

/// <summary>
/// A cross-platform MAUI control for viewing RDLC reports.
/// Renders reports as PDF in a WebView or provides direct PDF byte access.
/// </summary>
public partial class ReportViewer : ContentView, INotifyPropertyChanged
{
    #region Bindable Properties

    public static readonly BindableProperty ReportPathProperty =
        BindableProperty.Create(nameof(ReportPath), typeof(string), typeof(ReportViewer),
            propertyChanged: OnReportPathChanged);

    public static readonly BindableProperty ReportDataSourcesProperty =
        BindableProperty.Create(nameof(ReportDataSources), typeof(IList<MauiModels.ReportDataSourceInfo>), typeof(ReportViewer),
            defaultValue: new ObservableCollection<MauiModels.ReportDataSourceInfo>());

    public static readonly BindableProperty ParametersProperty =
        BindableProperty.Create(nameof(Parameters), typeof(IList<ReportParameter>), typeof(ReportViewer),
            defaultValue: new ObservableCollection<ReportParameter>());

    public static readonly BindableProperty ShowToolbarProperty =
        BindableProperty.Create(nameof(ShowToolbar), typeof(bool), typeof(ReportViewer), true,
            propertyChanged: OnShowToolbarChanged);

    public static readonly BindableProperty ShowExportButtonProperty =
        BindableProperty.Create(nameof(ShowExportButton), typeof(bool), typeof(ReportViewer), true);

    public static readonly BindableProperty ShowPrintButtonProperty =
        BindableProperty.Create(nameof(ShowPrintButton), typeof(bool), typeof(ReportViewer), true);

    public static readonly BindableProperty ShowRefreshButtonProperty =
        BindableProperty.Create(nameof(ShowRefreshButton), typeof(bool), typeof(ReportViewer), true);

    public static readonly BindableProperty ShowZoomControlsProperty =
        BindableProperty.Create(nameof(ShowZoomControls), typeof(bool), typeof(ReportViewer), true);

    public static readonly BindableProperty ZoomLevelProperty =
        BindableProperty.Create(nameof(ZoomLevel), typeof(int), typeof(ReportViewer), 100,
            propertyChanged: OnZoomLevelChanged);

    public static readonly BindableProperty CurrentPageProperty =
        BindableProperty.Create(nameof(CurrentPage), typeof(int), typeof(ReportViewer), 1,
            propertyChanged: OnCurrentPageChanged);

    public static readonly BindableProperty TotalPagesProperty =
        BindableProperty.Create(nameof(TotalPages), typeof(int), typeof(ReportViewer), 0);

    public static readonly BindableProperty IsBusyProperty =
        BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(ReportViewer), false);

    public static readonly BindableProperty StatusMessageProperty =
        BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(ReportViewer), string.Empty);

    public static readonly BindableProperty LocalReportProperty =
        BindableProperty.Create(nameof(LocalReport), typeof(LocalReport), typeof(ReportViewer));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the path to the RDLC report file.
    /// </summary>
    public string ReportPath
    {
        get => (string)GetValue(ReportPathProperty);
        set => SetValue(ReportPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the data sources for the report.
    /// </summary>
    public IList<MauiModels.ReportDataSourceInfo> ReportDataSources
    {
        get => (IList<MauiModels.ReportDataSourceInfo>)GetValue(ReportDataSourcesProperty);
        set => SetValue(ReportDataSourcesProperty, value);
    }

    /// <summary>
    /// Gets or sets the report parameters.
    /// </summary>
    public IList<ReportParameter> Parameters
    {
        get => (IList<ReportParameter>)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the toolbar is visible.
    /// </summary>
    public bool ShowToolbar
    {
        get => (bool)GetValue(ShowToolbarProperty);
        set => SetValue(ShowToolbarProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the export button is visible.
    /// </summary>
    public bool ShowExportButton
    {
        get => (bool)GetValue(ShowExportButtonProperty);
        set => SetValue(ShowExportButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the print button is visible.
    /// </summary>
    public bool ShowPrintButton
    {
        get => (bool)GetValue(ShowPrintButtonProperty);
        set => SetValue(ShowPrintButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the refresh button is visible.
    /// </summary>
    public bool ShowRefreshButton
    {
        get => (bool)GetValue(ShowRefreshButtonProperty);
        set => SetValue(ShowRefreshButtonProperty, value);
    }

    /// <summary>
    /// Gets or sets whether zoom controls are visible.
    /// </summary>
    public bool ShowZoomControls
    {
        get => (bool)GetValue(ShowZoomControlsProperty);
        set => SetValue(ShowZoomControlsProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom level (percentage).
    /// </summary>
    public int ZoomLevel
    {
        get => (int)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        private set => SetValue(TotalPagesProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the control is busy loading/rendering.
    /// </summary>
    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    public string StatusMessage
    {
        get => (string)GetValue(StatusMessageProperty);
        private set => SetValue(StatusMessageProperty, value);
    }

    /// <summary>
    /// Gets the underlying LocalReport instance for advanced scenarios.
    /// </summary>
    public LocalReport LocalReport
    {
        get => (LocalReport)GetValue(LocalReportProperty);
        private set => SetValue(LocalReportProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the report rendering is complete.
    /// </summary>
    public event EventHandler<MauiModels.ReportRenderingCompleteEventArgs>? RenderingComplete;

    /// <summary>
    /// Raised when an error occurs during report processing.
    /// </summary>
    public event EventHandler<MauiModels.ReportErrorEventArgs>? ReportError;

    /// <summary>
    /// Raised when the user requests to export the report.
    /// </summary>
    public event EventHandler<MauiModels.ReportExportEventArgs>? ExportRequested;

    /// <summary>
    /// Raised when the user requests to print the report.
    /// </summary>
    public event EventHandler<MauiModels.ReportPrintEventArgs>? PrintRequested;

    #endregion

    #region Private Fields

    private readonly WebView _webView;
    private readonly Grid _mainGrid;
    private readonly Grid _toolbarGrid;
    private readonly ActivityIndicator _loadingIndicator;
    private readonly Label _statusLabel;
    private readonly Label _pageLabel;
    private byte[]? _currentPdfData;
    private readonly ReportService _reportService;

    #endregion

    #region Constructor

    public ReportViewer()
    {
        _reportService = new ReportService();
        LocalReport = new LocalReport();

        // Build the UI
        _mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            }
        };

        // Toolbar
        _toolbarGrid = CreateToolbar();
        Grid.SetRow(_toolbarGrid, 0);
        _mainGrid.Add(_toolbarGrid);

        // WebView for PDF display
        _webView = new WebView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };
        Grid.SetRow(_webView, 1);
        _mainGrid.Add(_webView);

        // Loading indicator overlay
        _loadingIndicator = new ActivityIndicator
        {
            IsRunning = false,
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Color = Colors.Blue
        };
        Grid.SetRow(_loadingIndicator, 1);
        _mainGrid.Add(_loadingIndicator);

        // Status label
        _statusLabel = new Label
        {
            IsVisible = false,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            TextColor = Colors.Gray
        };
        Grid.SetRow(_statusLabel, 1);
        _mainGrid.Add(_statusLabel);

        // Page label
        _pageLabel = new Label
        {
            Text = "Page 0 of 0",
            VerticalOptions = LayoutOptions.Center
        };

        Content = _mainGrid;
    }

    #endregion

    #region Toolbar Creation

    private Grid CreateToolbar()
    {
        var toolbar = new Grid
        {
            Padding = new Thickness(8),
            Background = new SolidColorBrush(Color.FromArgb("#F5F5F5")),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto }, // Navigation
                new ColumnDefinition { Width = GridLength.Star }, // Spacer
                new ColumnDefinition { Width = GridLength.Auto }, // Zoom
                new ColumnDefinition { Width = GridLength.Auto }, // Actions
            }
        };

        // Navigation buttons
        var navStack = new HorizontalStackLayout { Spacing = 4 };
        
        var firstButton = CreateToolbarButton("⏮", "First Page");
        firstButton.Clicked += async (s, e) => await GoToFirstPageAsync();
        navStack.Add(firstButton);

        var prevButton = CreateToolbarButton("◀", "Previous Page");
        prevButton.Clicked += async (s, e) => await GoToPreviousPageAsync();
        navStack.Add(prevButton);

        _pageLabel.VerticalOptions = LayoutOptions.Center;
        _pageLabel.Margin = new Thickness(8, 0);
        navStack.Add(_pageLabel);

        var nextButton = CreateToolbarButton("▶", "Next Page");
        nextButton.Clicked += async (s, e) => await GoToNextPageAsync();
        navStack.Add(nextButton);

        var lastButton = CreateToolbarButton("⏭", "Last Page");
        lastButton.Clicked += async (s, e) => await GoToLastPageAsync();
        navStack.Add(lastButton);

        Grid.SetColumn(navStack, 0);
        toolbar.Add(navStack);

        // Zoom controls
        var zoomStack = new HorizontalStackLayout { Spacing = 4 };
        
        var zoomOutButton = CreateToolbarButton("➖", "Zoom Out");
        zoomOutButton.Clicked += (s, e) => ZoomLevel = Math.Max(25, ZoomLevel - 25);
        zoomStack.Add(zoomOutButton);

        var zoomLabel = new Label { Text = "100%", VerticalOptions = LayoutOptions.Center };
        zoomLabel.SetBinding(Label.TextProperty, new Binding(nameof(ZoomLevel), source: this, 
            converter: new ZoomLevelToTextConverter()));
        zoomStack.Add(zoomLabel);

        var zoomInButton = CreateToolbarButton("➕", "Zoom In");
        zoomInButton.Clicked += (s, e) => ZoomLevel = Math.Min(400, ZoomLevel + 25);
        zoomStack.Add(zoomInButton);

        Grid.SetColumn(zoomStack, 2);
        toolbar.Add(zoomStack);

        // Action buttons
        var actionStack = new HorizontalStackLayout { Spacing = 4 };

        var refreshButton = CreateToolbarButton("🔄", "Refresh");
        refreshButton.Clicked += async (s, e) => await RefreshReportAsync();
        refreshButton.SetBinding(IsVisibleProperty, new Binding(nameof(ShowRefreshButton), source: this));
        actionStack.Add(refreshButton);

        var exportButton = CreateToolbarButton("📥", "Export");
        exportButton.Clicked += async (s, e) => await ShowExportOptionsAsync();
        exportButton.SetBinding(IsVisibleProperty, new Binding(nameof(ShowExportButton), source: this));
        actionStack.Add(exportButton);

        var printButton = CreateToolbarButton("🖨️", "Print");
        printButton.Clicked += async (s, e) => await PrintAsync();
        printButton.SetBinding(IsVisibleProperty, new Binding(nameof(ShowPrintButton), source: this));
        actionStack.Add(printButton);

        Grid.SetColumn(actionStack, 3);
        toolbar.Add(actionStack);

        return toolbar;
    }

    private static Button CreateToolbarButton(string text, string tooltip)
    {
        return new Button
        {
            Text = text,
            Padding = new Thickness(8, 4),
            MinimumWidthRequest = 40,
            FontSize = 14,
            Background = Colors.Transparent,
            BorderWidth = 0
        };
    }

    #endregion

    #region Property Changed Handlers

    private static void OnReportPathChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ReportViewer viewer && newValue is string path && !string.IsNullOrEmpty(path))
        {
            viewer.LoadReportFromPathAsync(path).ConfigureAwait(false);
        }
    }

    private static void OnShowToolbarChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ReportViewer viewer)
        {
            viewer._toolbarGrid.IsVisible = (bool)newValue;
        }
    }

    private static void OnZoomLevelChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ReportViewer viewer)
        {
            viewer.ApplyZoom((int)newValue);
        }
    }

    private static void OnCurrentPageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ReportViewer viewer)
        {
            viewer.UpdatePageLabel();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads a report from a file path.
    /// </summary>
    public async Task LoadReportFromPathAsync(string path)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading report...";
            _loadingIndicator.IsVisible = true;
            _loadingIndicator.IsRunning = true;

            LocalReport = new LocalReport();

            if (File.Exists(path))
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                LocalReport.LoadReportDefinition(fs);
            }
            else
            {
                throw new FileNotFoundException($"Report file not found: {path}");
            }

            await SetupDataSourcesAndRenderAsync();
        }
        catch (Exception ex)
        {
            OnReportError(ex);
        }
        finally
        {
            IsBusy = false;
            _loadingIndicator.IsVisible = false;
            _loadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Loads a report from a stream.
    /// </summary>
    public async Task LoadReportFromStreamAsync(Stream stream)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading report...";
            _loadingIndicator.IsVisible = true;
            _loadingIndicator.IsRunning = true;

            LocalReport = new LocalReport();
            LocalReport.LoadReportDefinition(stream);

            await SetupDataSourcesAndRenderAsync();
        }
        catch (Exception ex)
        {
            OnReportError(ex);
        }
        finally
        {
            IsBusy = false;
            _loadingIndicator.IsVisible = false;
            _loadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Loads a report from an embedded resource.
    /// </summary>
    public async Task LoadReportFromEmbeddedResourceAsync(string resourceName, System.Reflection.Assembly? assembly = null)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading report...";
            _loadingIndicator.IsVisible = true;
            _loadingIndicator.IsRunning = true;

            assembly ??= System.Reflection.Assembly.GetCallingAssembly();
            LocalReport = new LocalReport();
            LocalReport.ReportEmbeddedResource = resourceName;

            await SetupDataSourcesAndRenderAsync();
        }
        catch (Exception ex)
        {
            OnReportError(ex);
        }
        finally
        {
            IsBusy = false;
            _loadingIndicator.IsVisible = false;
            _loadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Refreshes the report with current data sources and parameters.
    /// </summary>
    public async Task RefreshReportAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Refreshing report...";
            _loadingIndicator.IsVisible = true;
            _loadingIndicator.IsRunning = true;

            await RenderReportAsync();
        }
        catch (Exception ex)
        {
            OnReportError(ex);
        }
        finally
        {
            IsBusy = false;
            _loadingIndicator.IsVisible = false;
            _loadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Adds a data source to the report.
    /// </summary>
    public void AddDataSource(string name, object data)
    {
        LocalReport.DataSources.Add(new ReportDataSource(name, data));
    }

    /// <summary>
    /// Sets report parameters.
    /// </summary>
    public void SetParameters(IEnumerable<ReportParameter> parameters)
    {
        LocalReport.SetParameters(parameters);
    }

    /// <summary>
    /// Exports the report to the specified format.
    /// </summary>
    public async Task<byte[]> ExportAsync(string format, string? deviceInfo = null)
    {
        return await Task.Run(() => _reportService.RenderReport(LocalReport, format, deviceInfo));
    }

    /// <summary>
    /// Exports the report to a file.
    /// </summary>
    public async Task<string> ExportToFileAsync(string format, string? fileName = null)
    {
        var data = await ExportAsync(format);
        
        var extension = format.ToUpperInvariant() switch
        {
            "PDF" => ".pdf",
            "EXCELOPENXML" or "EXCEL" => ".xlsx",
            "WORDOPENXML" or "WORD" => ".docx",
            "IMAGE" => ".png",
            "HTML4.0" or "HTML5" => ".html",
            _ => ".bin"
        };

        fileName ??= $"Report_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
        
        await File.WriteAllBytesAsync(filePath, data);
        
        return filePath;
    }

    /// <summary>
    /// Gets the current rendered PDF data.
    /// </summary>
    public byte[]? GetCurrentPdfData() => _currentPdfData;

    #endregion

    #region Navigation Methods

    public async Task GoToFirstPageAsync()
    {
        if (TotalPages > 0)
        {
            CurrentPage = 1;
            await NavigateToPageAsync(1);
        }
    }

    public async Task GoToPreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await NavigateToPageAsync(CurrentPage);
        }
    }

    public async Task GoToNextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await NavigateToPageAsync(CurrentPage);
        }
    }

    public async Task GoToLastPageAsync()
    {
        if (TotalPages > 0)
        {
            CurrentPage = TotalPages;
            await NavigateToPageAsync(TotalPages);
        }
    }

    #endregion

    #region Private Methods

    private async Task SetupDataSourcesAndRenderAsync()
    {
        // Add data sources from the bound collection
        foreach (var ds in ReportDataSources)
        {
            LocalReport.DataSources.Add(new ReportDataSource(ds.Name, ds.Value));
        }

        // Set parameters
        if (Parameters.Any())
        {
            LocalReport.SetParameters(Parameters);
        }

        await RenderReportAsync();
    }

    private async Task RenderReportAsync()
    {
        try
        {
            StatusMessage = "Rendering report...";

            // Render to PDF
            _currentPdfData = await Task.Run(() => _reportService.RenderReport(LocalReport, "PDF"));

            if (_currentPdfData != null && _currentPdfData.Length > 0)
            {
                // Save to temp file and display in WebView
                var tempFile = Path.Combine(FileSystem.CacheDirectory, $"report_{Guid.NewGuid()}.pdf");
                await File.WriteAllBytesAsync(tempFile, _currentPdfData);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _webView.Source = tempFile;
                });

                // Estimate total pages (rough estimate based on PDF size)
                TotalPages = Math.Max(1, _currentPdfData.Length / 5000);
                CurrentPage = 1;
                UpdatePageLabel();

                StatusMessage = "Report loaded successfully.";
                RenderingComplete?.Invoke(this, new MauiModels.ReportRenderingCompleteEventArgs(true, TotalPages));
            }
            else
            {
                StatusMessage = "No data to display.";
                _statusLabel.Text = StatusMessage;
                _statusLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            OnReportError(ex);
        }
    }

    private async Task NavigateToPageAsync(int page)
    {
        // For PDF displayed in WebView, page navigation is handled by the PDF viewer
        // This is a placeholder for future implementation with custom PDF rendering
        UpdatePageLabel();
        await Task.CompletedTask;
    }

    private void ApplyZoom(int zoomLevel)
    {
        // Apply zoom via WebView JavaScript for PDF viewing
        // Implementation depends on the PDF viewer being used
    }

    private void UpdatePageLabel()
    {
        _pageLabel.Text = $"Page {CurrentPage} of {TotalPages}";
    }

    private async Task ShowExportOptionsAsync()
    {
        var formats = new[] { "PDF", "Excel (XLSX)", "Word (DOCX)", "HTML" };
        
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return;

        var result = await page.DisplayActionSheet("Export Format", "Cancel", null, formats);
        
        if (string.IsNullOrEmpty(result) || result == "Cancel") return;

        var format = result switch
        {
            "PDF" => "PDF",
            "Excel (XLSX)" => "EXCELOPENXML",
            "Word (DOCX)" => "WORDOPENXML",
            "HTML" => "HTML5",
            _ => "PDF"
        };

        var eventArgs = new MauiModels.ReportExportEventArgs(format);
        ExportRequested?.Invoke(this, eventArgs);

        if (!eventArgs.Handled)
        {
            try
            {
                var filePath = await ExportToFileAsync(format);
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Share Report",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                OnReportError(ex);
            }
        }
    }

    private async Task PrintAsync()
    {
        var eventArgs = new MauiModels.ReportPrintEventArgs();
        PrintRequested?.Invoke(this, eventArgs);

        if (!eventArgs.Handled)
        {
            // Default print behavior - export to PDF and share/open
            try
            {
                var filePath = await ExportToFileAsync("PDF");
                
                // Use platform-specific printing or share
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
            catch (Exception ex)
            {
                OnReportError(ex);
            }
        }
    }

    private void OnReportError(Exception ex)
    {
        StatusMessage = $"Error: {ex.Message}";
        _statusLabel.Text = StatusMessage;
        _statusLabel.IsVisible = true;
        ReportError?.Invoke(this, new MauiModels.ReportErrorEventArgs(ex));
    }

    #endregion
}

/// <summary>
/// Converter for zoom level to display text.
/// </summary>
internal class ZoomLevelToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is int zoomLevel)
        {
            return $"{zoomLevel}%";
        }
        return "100%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
