# Getting Started with ReportViewerCore

## Installation

### NuGet Package

Install the appropriate package for your application type:

```bash
# For ASP.NET Core, Blazor, MAUI, and Console applications
dotnet add package Microsoft.Reporting.NETCore

# For WinForms applications (includes UI controls)
dotnet add package Microsoft.Reporting.WinForms
```

### Project Configuration

Your project file should target one of the supported frameworks:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

## Basic Concepts

### LocalReport

`LocalReport` is used for client-side report processing with RDLC files:

- Processes reports locally without requiring a Reporting Services server
- Loads report definitions from files, embedded resources, or streams
- Binds to in-memory data sources
- Suitable for most scenarios in web and desktop applications

### ServerReport

`ServerReport` is used for server-side report processing with SQL Server Reporting Services:

- Connects to a Reporting Services server
- Executes reports hosted on the server
- Handles authentication and credentials
- Ideal for enterprise scenarios with centralized report management

### Report Data Sources

Reports require data sources to populate their content:

```csharp
// Create data source from any IEnumerable
var data = new List<InvoiceItem> {
    new InvoiceItem { Description = "Product A", Price = 99.99m, Quantity = 2 },
    new InvoiceItem { Description = "Product B", Price = 149.99m, Quantity = 1 }
};

report.DataSources.Add(new ReportDataSource("InvoiceItems", data));
```

### Report Parameters

Parameters allow you to customize report output:

```csharp
report.SetParameters(new[] {
    new ReportParameter("ReportTitle", "Monthly Sales Report"),
    new ReportParameter("ReportDate", DateTime.Now.ToString("yyyy-MM-dd")),
    new ReportParameter("ShowDetails", "true")
});
```

## Creating Your First Report

### Step 1: Create an RDLC File

1. Install the RDLC Report Designer extension for Visual Studio:
   - **VS 2019**: [Microsoft RDLC Report Designer](https://marketplace.visualstudio.com/items?itemName=ProBITools.MicrosoftRdlcReportDesignerforVisualStudio-18001)
   - **VS 2022**: [Microsoft RDLC Report Designer 2022](https://marketplace.visualstudio.com/items?itemName=ProBITools.MicrosoftRdlcReportDesignerforVisualStudio2022)

2. Add a new Report item to your project (Add > New Item > Report)

3. Design your report using the visual designer

### Step 2: Create Data Model

```csharp
public class ReportItem
{
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total => Price * Quantity;
}
```

### Step 3: Generate Report

```csharp
using Microsoft.Reporting.NETCore;
using System.IO;

public class ReportGenerator
{
    public byte[] GenerateInvoice(List<ReportItem> items)
    {
        using var report = new LocalReport();
        
        // Load RDLC file
        using var rdlcStream = File.OpenRead("Reports/Invoice.rdlc");
        report.LoadReportDefinition(rdlcStream);
        
        // Add data source
        report.DataSources.Add(new ReportDataSource("Items", items));
        
        // Set parameters
        report.SetParameters(new[] {
            new ReportParameter("InvoiceNumber", "INV-001"),
            new ReportParameter("InvoiceDate", DateTime.Now.ToString("MM/dd/yyyy"))
        });
        
        // Render to PDF
        return report.Render("PDF");
    }
}
```

## Rendering Formats

ReportViewerCore supports multiple output formats:

| Format | Extension | MIME Type | Description |
|--------|-----------|-----------|-------------|
| PDF | .pdf | application/pdf | Portable Document Format |
| EXCELOPENXML | .xlsx | application/vnd.openxmlformats-officedocument.spreadsheetml.sheet | Excel 2007+ |
| WORDOPENXML | .docx | application/vnd.openxmlformats-officedocument.wordprocessingml.document | Word 2007+ |
| HTML5 | .html | text/html | HTML5 (works without JavaScript) |
| HTML4.0 | .html | text/html | HTML 4.0 |
| IMAGE | .tiff | image/tiff | TIFF image |
| EXCEL | .xls | application/vnd.ms-excel | Excel 97-2003 |
| WORD | .doc | application/msword | Word 97-2003 |

### Rendering Example

```csharp
// Render to different formats
byte[] pdf = report.Render("PDF");
byte[] excel = report.Render("EXCELOPENXML");
byte[] word = report.Render("WORDOPENXML");
byte[] html = report.Render("HTML5");

// Get additional rendering information
string mimeType, encoding, fileExtension;
string[] streamIds;
Warning[] warnings;

byte[] output = report.Render(
    format: "PDF",
    deviceInfo: null,
    pageCountMode: PageCountMode.Actual,
    out mimeType,
    out encoding,
    out fileExtension,
    out streamIds,
    out warnings
);
```

## Working with Embedded Resources

To embed RDLC files in your assembly:

1. Set the RDLC file's Build Action to "Embedded Resource"

2. Load the embedded resource:

```csharp
using var stream = Assembly.GetExecutingAssembly()
    .GetManifestResourceStream("YourNamespace.Reports.Invoice.rdlc");
report.LoadReportDefinition(stream);
```

## Linux/macOS Support

For PDF, Excel (XLS), and TIFF rendering on Linux/macOS:

1. Install Wine 5.0 or newer:
   ```bash
   # Debian/Ubuntu
   sudo apt install wine
   ```

2. Download Windows .NET runtime binaries

3. Run your application with Wine:
   ```bash
   wine64 ~/dotnet-windows/dotnet.exe YourApp.dll
   ```

Alternatively, use OpenXML formats (EXCELOPENXML, WORDOPENXML) which work natively on all platforms.

## Best Practices

1. **Dispose reports properly**: Always use `using` statements or call `Dispose()`
2. **Cache RDLC files**: Load report definitions once and reuse when possible
3. **Use OpenXML formats**: They work consistently across all platforms
4. **Validate parameters**: Check parameter values before setting them
5. **Handle warnings**: Check the warnings array after rendering
6. **Use appropriate data types**: Match your data model to the report schema

## Next Steps

- [ASP.NET Core Integration](02-ASP-NET-Core-Integration.md) - Learn how to use reports in web APIs
- [Blazor Integration](03-Blazor-Integration.md) - Display reports in Blazor applications
- [MAUI Integration](04-MAUI-Integration.md) - Generate reports in mobile apps
- [Advanced Topics](06-Advanced-Topics.md) - Subreports, custom code, and more
