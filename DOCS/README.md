# ReportViewerCore Documentation

This documentation provides comprehensive guidance on using Microsoft ReportViewerCore with modern .NET applications.

## Table of Contents

1. [Getting Started](01-Getting-Started.md)
2. [ASP.NET Core Integration](02-ASP-NET-Core-Integration.md)
3. [Blazor Integration](03-Blazor-Integration.md)
4. [MAUI Integration](04-MAUI-Integration.md)
5. [API Mode vs Standalone Mode](05-API-vs-Standalone-Mode.md)
6. [Advanced Topics](06-Advanced-Topics.md)
7. [Troubleshooting](07-Troubleshooting.md)

## Overview

ReportViewerCore is a port of Microsoft Reporting Services (Report Viewer) to .NET Core 3.1+. It enables you to:

- Generate reports programmatically in .NET applications
- Render reports in multiple formats (PDF, Excel, Word, HTML)
- Use both local report processing (RDLC files) and server-side processing (Reporting Services)
- Support desktop (WinForms), web (ASP.NET Core, Blazor), and mobile (MAUI) applications

## Key Features

- **Multi-platform support**: Windows, Linux, and macOS
- **Multiple rendering formats**: PDF, Excel, Word, HTML, Image
- **Local processing**: Use RDLC files without a Reporting Services server
- **Server processing**: Connect to SQL Server Reporting Services
- **No UI dependencies**: Perfect for API and headless scenarios

## Quick Example

```csharp
using Microsoft.Reporting.NETCore;

// Create a local report
var report = new LocalReport();

// Load the report definition
using var rdlcStream = File.OpenRead("Report.rdlc");
report.LoadReportDefinition(rdlcStream);

// Add data sources
report.DataSources.Add(new ReportDataSource("DataSet1", myData));

// Set parameters
report.SetParameters(new[] { 
    new ReportParameter("Title", "My Report") 
});

// Render to PDF
byte[] pdf = report.Render("PDF");

// Save or return the PDF
File.WriteAllBytes("output.pdf", pdf);
```

## Supported Platforms

- .NET Core 3.1
- .NET 5
- .NET 6
- .NET 7
- .NET 8

## License

This is a community port and is NOT officially supported by Microsoft.
