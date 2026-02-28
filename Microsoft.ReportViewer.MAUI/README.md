# Microsoft.ReportViewer.MAUI

A cross-platform .NET MAUI control for viewing and exporting RDLC (Report Definition Language Client-side) reports.

## Platform Support

| Platform | Support | Notes |
|----------|---------|-------|
| Windows | ✅ Full | Full PDF rendering and printing |
| Android | ✅ Full | PDF export and viewing |
| iOS | ✅ Full | PDF export and sharing |
| macOS | ✅ Full | PDF export and viewing |

## Installation

Add a reference to the project or install via NuGet:

```bash
dotnet add package ReportViewerCore.MAUI
```

## Quick Start

### XAML Usage

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:rv="clr-namespace:Microsoft.ReportViewer.MAUI.Controls;assembly=Microsoft.ReportViewer.MAUI"
             x:Class="YourApp.ReportPage">
    
    <rv:ReportViewer x:Name="reportViewer"
                     ShowToolbar="True"
                     ShowExportButton="True"
                     ShowPrintButton="True"
                     RenderingComplete="OnRenderingComplete"
                     ReportError="OnReportError" />
</ContentPage>
```

### Code-Behind

```csharp
using Microsoft.Reporting.NETCore;
using Microsoft.ReportViewer.MAUI.Controls;

public partial class ReportPage : ContentPage
{
    public ReportPage()
    {
        InitializeComponent();
        LoadReport();
    }

    private async void LoadReport()
    {
        // Option 1: Load from file path
        await reportViewer.LoadReportFromPathAsync("Reports/Invoice.rdlc");
        
        // Add data source
        var items = GetInvoiceItems();
        reportViewer.AddDataSource("InvoiceItems", items);
        
        // Set parameters
        reportViewer.SetParameters(new[]
        {
            new ReportParameter("InvoiceNumber", "INV-001"),
            new ReportParameter("InvoiceDate", DateTime.Now.ToString("yyyy-MM-dd"))
        });
        
        // Refresh to render with data
        await reportViewer.RefreshReportAsync();
    }

    private void OnRenderingComplete(object sender, ReportRenderingCompleteEventArgs e)
    {
        if (e.Success)
        {
            Console.WriteLine($"Report rendered with {e.TotalPages} pages.");
        }
    }

    private void OnReportError(object sender, ReportErrorEventArgs e)
    {
        DisplayAlert("Error", e.Exception.Message, "OK");
        e.Handled = true;
    }
}
```

## Using ReportService Directly

For headless report generation without UI:

```csharp
using Microsoft.ReportViewer.MAUI.Services;

public class InvoiceService
{
    private readonly ReportService _reportService = new();

    public async Task<byte[]> GenerateInvoicePdfAsync(Invoice invoice)
    {
        var report = new LocalReport();
        
        using var stream = await FileSystem.OpenAppPackageFileAsync("Reports/Invoice.rdlc");
        report.LoadReportDefinition(stream);
        
        report.DataSources.Add(new ReportDataSource("Items", invoice.Items));
        report.SetParameters(new[]
        {
            new ReportParameter("InvoiceNumber", invoice.Number),
            new ReportParameter("CustomerName", invoice.CustomerName)
        });
        
        return report.Render("PDF");
    }

    public async Task ShareInvoiceAsync(byte[] pdfData, string invoiceNumber)
    {
        await _reportService.ShareReportAsync(
            pdfData, 
            $"Invoice_{invoiceNumber}.pdf",
            "Share Invoice"
        );
    }
}
```

## Export Formats

| Format | Extension | Description |
|--------|-----------|-------------|
| `PDF` | .pdf | Adobe PDF Document |
| `EXCELOPENXML` | .xlsx | Microsoft Excel Workbook |
| `WORDOPENXML` | .docx | Microsoft Word Document |
| `HTML5` | .html | HTML Web Page |
| `IMAGE` | .png | PNG Image |

## Features

### Toolbar Controls
- Page navigation (first, previous, next, last)
- Zoom in/out
- Export to multiple formats
- Print support (platform-dependent)
- Refresh button

### Programmatic Control
- Load reports from file, stream, or embedded resource
- Add multiple data sources
- Set report parameters
- Export to various formats
- Share reports via system share dialog
- Open reports in default application

### Events
- `RenderingComplete` - Fired when report is fully rendered
- `ReportError` - Fired when an error occurs
- `ExportRequested` - Fired when user requests export
- `PrintRequested` - Fired when user requests print

## Platform-Specific Notes

### Windows
Full printing support via system print dialog.

### Android
Uses PDF viewer in WebView. Sharing uses Android's share intent.

### iOS
PDF viewing via native viewer. Sharing uses iOS share sheet.

### macOS
PDF viewing in WebView. Printing via Catalyst APIs.

## Dependencies

- Microsoft.Reporting.NETCore (report processing engine)
- CommunityToolkit.Maui (MAUI helpers)
- CommunityToolkit.Mvvm (MVVM support)

## License

Same license as ReportViewerCore main package.
