# MAUI Integration

This guide covers how to integrate ReportViewerCore with .NET MAUI applications for cross-platform mobile and desktop report generation.

## Overview

.NET MAUI (Multi-platform App UI) allows you to build cross-platform apps for Android, iOS, Windows, and macOS. ReportViewerCore works with MAUI to generate reports on all these platforms.

## Installation

```bash
dotnet add package Microsoft.Reporting.NETCore
```

## Platform Support

| Platform | Local Reports | Server Reports | PDF | Excel | Word | HTML |
|----------|---------------|----------------|-----|-------|------|------|
| Windows | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Android | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |
| iOS | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |
| macOS | ✅ | ✅ | ⚠️ | ✅ | ✅ | ✅ |

⚠️ = Requires additional setup (Wine or alternative renderers). OpenXML formats (EXCELOPENXML, WORDOPENXML) work without issues.

## Architecture Patterns

### Pattern 1: Embedded Report Generation (Recommended)

Generate reports directly in the MAUI app:

```csharp
// Services/ReportService.cs
public class MauiReportService
{
    public byte[] GenerateReport<T>(
        string reportName,
        IEnumerable<T> data,
        Dictionary<string, string> parameters = null,
        string format = "PDF")
    {
        using var report = new LocalReport();
        
        // Load RDLC from embedded resource
        var assembly = typeof(MauiReportService).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            $"YourApp.Reports.{reportName}.rdlc");
        
        if (stream == null)
            throw new FileNotFoundException($"Report {reportName}.rdlc not found");
        
        report.LoadReportDefinition(stream);
        
        // Add data source
        report.DataSources.Add(new ReportDataSource("DataSet1", data));
        
        // Set parameters
        if (parameters != null)
        {
            var reportParams = parameters
                .Select(p => new ReportParameter(p.Key, p.Value))
                .ToArray();
            report.SetParameters(reportParams);
        }
        
        // Render
        return report.Render(format);
    }
    
    public async Task<string> SaveReportToFileAsync(
        byte[] reportData, 
        string fileName)
    {
        var folderPath = FileSystem.AppDataDirectory;
        var filePath = Path.Combine(folderPath, fileName);
        
        await File.WriteAllBytesAsync(filePath, reportData);
        
        return filePath;
    }
    
    public async Task<bool> ShareReportAsync(string filePath, string title)
    {
        if (!File.Exists(filePath))
            return false;
        
        await Share.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(filePath)
        });
        
        return true;
    }
}
```

### Pattern 2: API-Based Generation

Use a backend API for report generation (useful for complex reports):

```csharp
// Services/ApiReportService.cs
public class ApiReportService
{
    private readonly HttpClient _httpClient;
    
    public ApiReportService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<byte[]> GenerateReportAsync(
        string reportName,
        object requestData,
        string format = "PDF")
    {
        var request = new
        {
            ReportName = reportName,
            Format = format,
            Data = requestData
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "api/reports/generate", 
            request);
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsByteArrayAsync();
    }
    
    public async Task<byte[]> DownloadReportAsync(
        string reportEndpoint)
    {
        var response = await _httpClient.GetAsync(reportEndpoint);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsByteArrayAsync();
    }
}
```

## MAUI Implementation Examples

### Page with Report Generation

```csharp
// Pages/InvoicePage.xaml.cs
public partial class InvoicePage : ContentPage
{
    private readonly MauiReportService _reportService;
    private readonly IInvoiceDataService _dataService;
    
    public InvoicePage(
        MauiReportService reportService,
        IInvoiceDataService dataService)
    {
        InitializeComponent();
        _reportService = reportService;
        _dataService = dataService;
    }
    
    private async void OnGeneratePdfClicked(object sender, EventArgs e)
    {
        await GenerateAndShowReport("PDF");
    }
    
    private async void OnGenerateExcelClicked(object sender, EventArgs e)
    {
        await GenerateAndShowReport("EXCELOPENXML");
    }
    
    private async Task GenerateAndShowReport(string format)
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            
            // Get invoice data
            var invoiceId = int.Parse(InvoiceIdEntry.Text);
            var invoice = await _dataService.GetInvoiceAsync(invoiceId);
            
            // Generate report
            var parameters = new Dictionary<string, string>
            {
                ["InvoiceNumber"] = invoice.InvoiceNumber,
                ["InvoiceDate"] = invoice.Date.ToString("MM/dd/yyyy"),
                ["CustomerName"] = invoice.CustomerName
            };
            
            var reportData = _reportService.GenerateReport(
                "Invoice",
                invoice.Items,
                parameters,
                format
            );
            
            // Save to file
            var extension = format == "EXCELOPENXML" ? "xlsx" : "pdf";
            var fileName = $"Invoice_{invoiceId}.{extension}";
            var filePath = await _reportService.SaveReportToFileAsync(
                reportData, 
                fileName);
            
            // Share or open the file
            await _reportService.ShareReportAsync(
                filePath, 
                $"Invoice {invoiceId}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }
}
```

### XAML Layout

```xml
<!-- Pages/InvoicePage.xaml -->
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="YourApp.Pages.InvoicePage"
             Title="Generate Invoice">
    
    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="20">
            
            <Label Text="Generate Invoice Report"
                   FontSize="24"
                   FontAttributes="Bold" />
            
            <Frame BorderColor="LightGray" Padding="10">
                <VerticalStackLayout Spacing="10">
                    <Label Text="Invoice ID:" />
                    <Entry x:Name="InvoiceIdEntry"
                           Placeholder="Enter invoice ID"
                           Keyboard="Numeric" />
                </VerticalStackLayout>
            </Frame>
            
            <Button Text="Generate PDF"
                    Clicked="OnGeneratePdfClicked"
                    BackgroundColor="#007bff"
                    TextColor="White" />
            
            <Button Text="Generate Excel"
                    Clicked="OnGenerateExcelClicked"
                    BackgroundColor="#28a745"
                    TextColor="White" />
            
            <ActivityIndicator x:Name="LoadingIndicator"
                             IsVisible="False"
                             IsRunning="False"
                             Color="Blue" />
            
        </VerticalStackLayout>
    </ScrollView>
    
</ContentPage>
```

### ViewModel Pattern (MVVM)

```csharp
// ViewModels/ReportViewModel.cs
public class ReportViewModel : INotifyPropertyChanged
{
    private readonly MauiReportService _reportService;
    private bool _isBusy;
    private string _statusMessage;
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }
    
    public ICommand GeneratePdfCommand { get; }
    public ICommand GenerateExcelCommand { get; }
    public ICommand GenerateWordCommand { get; }
    
    public ReportViewModel(MauiReportService reportService)
    {
        _reportService = reportService;
        
        GeneratePdfCommand = new Command(async () => await GenerateReport("PDF"));
        GenerateExcelCommand = new Command(async () => await GenerateReport("EXCELOPENXML"));
        GenerateWordCommand = new Command(async () => await GenerateReport("WORDOPENXML"));
    }
    
    private async Task GenerateReport(string format)
    {
        if (IsBusy) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Generating report...";
            
            // Get data
            var data = await GetReportDataAsync();
            
            // Generate report
            var reportBytes = _reportService.GenerateReport(
                "SalesReport",
                data,
                GetParameters(),
                format
            );
            
            // Save and share
            var extension = GetExtension(format);
            var fileName = $"Report_{DateTime.Now:yyyyMMdd}.{extension}";
            var filePath = await _reportService.SaveReportToFileAsync(
                reportBytes, 
                fileName);
            
            StatusMessage = "Report generated successfully!";
            
            await _reportService.ShareReportAsync(filePath, "Report");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private Dictionary<string, string> GetParameters()
    {
        return new Dictionary<string, string>
        {
            ["ReportDate"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["GeneratedBy"] = "Mobile App"
        };
    }
    
    private string GetExtension(string format) => format.ToUpper() switch
    {
        "EXCELOPENXML" => "xlsx",
        "WORDOPENXML" => "docx",
        _ => "pdf"
    };
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

## Platform-Specific Considerations

### Android

```csharp
// Platforms/Android/Services/AndroidReportService.cs
#if ANDROID
public class AndroidReportService : MauiReportService
{
    public async Task OpenReportAsync(string filePath)
    {
        var file = new Java.IO.File(filePath);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
            Platform.CurrentActivity,
            $"{Platform.CurrentActivity.PackageName}.fileprovider",
            file
        );
        
        var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
        intent.SetDataAndType(uri, GetMimeType(filePath));
        intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
        
        Platform.CurrentActivity.StartActivity(intent);
    }
    
    private string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "*/*"
        };
    }
}
#endif
```

Add FileProvider to AndroidManifest.xml:

```xml
<application>
    <provider
        android:name="androidx.core.content.FileProvider"
        android:authorities="${applicationId}.fileprovider"
        android:exported="false"
        android:grantUriPermissions="true">
        <meta-data
            android:name="android.support.FILE_PROVIDER_PATHS"
            android:resource="@xml/file_paths" />
    </provider>
</application>
```

Create file_paths.xml:

```xml
<!-- Platforms/Android/Resources/xml/file_paths.xml -->
<?xml version="1.0" encoding="utf-8"?>
<paths>
    <external-files-path name="app_files" path="." />
    <cache-path name="app_cache" path="." />
</paths>
```

### iOS

```csharp
// Platforms/iOS/Services/iOSReportService.cs
#if IOS
public class iOSReportService : MauiReportService
{
    public async Task OpenReportAsync(string filePath)
    {
        var url = Foundation.NSUrl.FromFilename(filePath);
        var documentController = UIKit.UIDocumentInteractionController.FromUrl(url);
        
        var viewController = Platform.GetCurrentUIViewController();
        documentController.PresentPreview(true);
    }
}
#endif
```

### Windows

```csharp
// Platforms/Windows/Services/WindowsReportService.cs
#if WINDOWS
public class WindowsReportService : MauiReportService
{
    public async Task OpenReportAsync(string filePath)
    {
        await Windows.System.Launcher.LaunchFileAsync(
            await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath));
    }
}
#endif
```

## Service Registration

```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register report services
#if ANDROID
        builder.Services.AddSingleton<MauiReportService, AndroidReportService>();
#elif IOS
        builder.Services.AddSingleton<MauiReportService, iOSReportService>();
#elif WINDOWS
        builder.Services.AddSingleton<MauiReportService, WindowsReportService>();
#else
        builder.Services.AddSingleton<MauiReportService>();
#endif

        // Register pages and view models
        builder.Services.AddTransient<InvoicePage>();
        builder.Services.AddTransient<ReportViewModel>();
        
        // Register data services
        builder.Services.AddScoped<IInvoiceDataService, InvoiceDataService>();

        return builder.Build();
    }
}
```

## Embedding RDLC Files

Set the Build Action to "Embedded Resource":

```xml
<!-- YourApp.csproj -->
<ItemGroup>
    <EmbeddedResource Include="Reports\Invoice.rdlc" />
    <EmbeddedResource Include="Reports\SalesReport.rdlc" />
    <EmbeddedResource Include="Reports\InventoryReport.rdlc" />
</ItemGroup>
```

## Background Report Generation

For long-running reports:

```csharp
public class BackgroundReportService
{
    private readonly MauiReportService _reportService;
    
    public BackgroundReportService(MauiReportService reportService)
    {
        _reportService = reportService;
    }
    
    public async Task<byte[]> GenerateReportInBackgroundAsync<T>(
        string reportName,
        IEnumerable<T> data,
        Dictionary<string, string> parameters,
        string format,
        IProgress<int> progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            progress?.Report(25);
            
            // Simulate data processing
            await Task.Delay(500, cancellationToken);
            progress?.Report(50);
            
            // Generate report
            var report = _reportService.GenerateReport(
                reportName,
                data,
                parameters,
                format
            );
            
            progress?.Report(100);
            
            return report;
        }, cancellationToken);
    }
}
```

Usage:

```csharp
var progress = new Progress<int>(percent =>
{
    ProgressBar.Progress = percent / 100.0;
    StatusLabel.Text = $"Generating report... {percent}%";
});

var cts = new CancellationTokenSource();
CancelButton.Clicked += (s, e) => cts.Cancel();

try
{
    var report = await _backgroundReportService.GenerateReportInBackgroundAsync(
        "Invoice",
        data,
        parameters,
        "PDF",
        progress,
        cts.Token
    );
    
    // Handle completed report
}
catch (OperationCanceledException)
{
    await DisplayAlert("Cancelled", "Report generation was cancelled", "OK");
}
```

## Offline Report Generation

Cache data for offline report generation:

```csharp
public class OfflineReportService
{
    private readonly MauiReportService _reportService;
    private readonly string _cacheDirectory;
    
    public OfflineReportService(MauiReportService reportService)
    {
        _reportService = reportService;
        _cacheDirectory = Path.Combine(
            FileSystem.AppDataDirectory, 
            "ReportCache");
        
        Directory.CreateDirectory(_cacheDirectory);
    }
    
    public async Task CacheDataAsync<T>(string key, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var filePath = Path.Combine(_cacheDirectory, $"{key}.json");
        await File.WriteAllTextAsync(filePath, json);
    }
    
    public async Task<T> GetCachedDataAsync<T>(string key)
    {
        var filePath = Path.Combine(_cacheDirectory, $"{key}.json");
        
        if (!File.Exists(filePath))
            return default;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }
    
    public async Task<byte[]> GenerateOfflineReportAsync(
        string reportName,
        string cacheKey,
        string format = "PDF")
    {
        // Get cached data
        var data = await GetCachedDataAsync<List<object>>(cacheKey);
        
        if (data == null)
            throw new InvalidOperationException("No cached data available");
        
        // Generate report
        return _reportService.GenerateReport(reportName, data, null, format);
    }
}
```

## Best Practices

1. **Use OpenXML formats**: They work consistently across all platforms without additional dependencies
2. **Embed RDLC files**: Include reports as embedded resources for easier deployment
3. **Implement progress indicators**: Report generation can take time on mobile devices
4. **Handle platform differences**: Use platform-specific services when needed
5. **Cache report data**: Enable offline report generation
6. **Optimize data loading**: Only load data needed for the report
7. **Test on physical devices**: Emulators may not reflect actual performance
8. **Handle permissions**: Request storage permissions on Android for file operations

## Next Steps

- [API vs Standalone Mode](05-API-vs-Standalone-Mode.md) - Choose the right architecture
- [Advanced Topics](06-Advanced-Topics.md) - Subreports, custom code, and more
- [Troubleshooting](07-Troubleshooting.md) - Common issues and solutions
