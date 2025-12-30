# API Mode vs Standalone Mode

This guide explains the different deployment and usage modes for ReportViewerCore and helps you choose the right approach for your application.

## Overview

ReportViewerCore can be deployed in two primary modes:

1. **Standalone Mode**: Report generation happens directly within your application
2. **API Mode**: Report generation is centralized in a dedicated API service

## Standalone Mode

### Description

In standalone mode, the ReportViewerCore library is embedded directly into your application. Report generation happens in-process within the same application that requests the report.

### Architecture

```
┌─────────────────────────┐
│   Your Application      │
│  ┌──────────────────┐   │
│  │  Business Logic  │   │
│  └────────┬─────────┘   │
│           │             │
│  ┌────────▼─────────┐   │
│  │ ReportViewerCore │   │
│  │   (Embedded)     │   │
│  └────────┬─────────┘   │
│           │             │
│      ┌────▼─────┐       │
│      │  Report  │       │
│      │  Output  │       │
│      └──────────┘       │
└─────────────────────────┘
```

### When to Use

✅ **Use Standalone Mode when:**

- Building desktop applications (WinForms, WPF, MAUI)
- Simple web applications with low report volume
- Single-tenant applications
- You need offline report generation
- Reports are tightly coupled to application logic
- You want minimal architectural complexity

❌ **Avoid Standalone Mode when:**

- Building Blazor WebAssembly applications (not supported)
- High-volume report generation is required
- Multiple applications need reports
- You need to scale report generation independently
- Reports consume significant server resources

### Implementation

#### Console Application

```csharp
class Program
{
    static void Main(string[] args)
    {
        var generator = new ReportGenerator();
        var invoice = GetInvoiceData(1001);
        
        var pdf = generator.GenerateInvoice(invoice, "PDF");
        File.WriteAllBytes("invoice.pdf", pdf);
        
        Console.WriteLine("Report generated successfully!");
    }
}

public class ReportGenerator
{
    public byte[] GenerateInvoice(Invoice invoice, string format)
    {
        using var report = new LocalReport();
        
        // Load report from file
        using var stream = File.OpenRead("Reports/Invoice.rdlc");
        report.LoadReportDefinition(stream);
        
        // Add data
        report.DataSources.Add(new ReportDataSource("Invoice", new[] { invoice }));
        report.DataSources.Add(new ReportDataSource("Items", invoice.Items));
        
        // Render
        return report.Render(format);
    }
}
```

#### ASP.NET Core (Standalone)

```csharp
// Simple page handler
public class ReportModel : PageModel
{
    public IActionResult OnGet(int invoiceId)
    {
        // Generate report directly in the page handler
        using var report = new LocalReport();
        LoadReport(report, invoiceId);
        
        var pdf = report.Render("PDF");
        return File(pdf, "application/pdf", $"Invoice_{invoiceId}.pdf");
    }
    
    private void LoadReport(LocalReport report, int invoiceId)
    {
        var data = GetInvoiceData(invoiceId);
        
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("MyApp.Reports.Invoice.rdlc");
        
        report.LoadReportDefinition(stream);
        report.DataSources.Add(new ReportDataSource("Items", data.Items));
        report.SetParameters(new[] {
            new ReportParameter("InvoiceNumber", data.InvoiceNumber)
        });
    }
}
```

#### MAUI Application

```csharp
public class MauiReportService
{
    public byte[] GenerateReport(string reportName, object data)
    {
        using var report = new LocalReport();
        
        // Load from embedded resource
        var assembly = typeof(MauiReportService).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            $"MyApp.Reports.{reportName}.rdlc");
        
        report.LoadReportDefinition(stream);
        report.DataSources.Add(new ReportDataSource("Data", data));
        
        return report.Render("EXCELOPENXML"); // Excel works well cross-platform
    }
}
```

### Advantages

✅ **Simpler deployment**: No separate API service needed
✅ **Lower latency**: No network overhead
✅ **Offline capable**: Works without internet connection
✅ **Easier debugging**: Everything in one process
✅ **Direct data access**: Can query database directly

### Disadvantages

❌ **Resource consumption**: Uses application's memory and CPU
❌ **Scalability limitations**: Limited to application's capacity
❌ **Code duplication**: Report logic repeated across apps
❌ **Update complexity**: Need to redeploy entire application for report changes
❌ **Not suitable for WASM**: Cannot run in browser

## API Mode

### Description

In API mode, report generation is centralized in a dedicated REST API service. Applications make HTTP requests to the API to generate reports.

### Architecture

```
┌──────────────┐      ┌──────────────────┐
│  Web App     │      │                  │
│              ├─────►│                  │
└──────────────┘      │                  │
                      │   Report API     │
┌──────────────┐      │   Service        │
│  Mobile App  │      │                  │
│              ├─────►│  ┌────────────┐  │
└──────────────┘      │  │ ReportCore │  │
                      │  └────────────┘  │
┌──────────────┐      │                  │
│  Desktop App │      │  ┌────────────┐  │
│              ├─────►│  │  Reports   │  │
└──────────────┘      │  └────────────┘  │
                      │                  │
                      └──────────────────┘
```

### When to Use

✅ **Use API Mode when:**

- Building Blazor WebAssembly applications
- Multiple applications need reports
- High-volume report generation
- Need to scale report generation independently
- Want centralized report management
- Reports are resource-intensive
- Microservices architecture

❌ **Avoid API Mode when:**

- Building simple desktop applications
- Network connectivity is unreliable
- Reports need immediate/offline access
- Additional infrastructure is undesirable

### Implementation

#### Dedicated Report API

```csharp
// ReportAPI/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IReportGenerator, ReportGenerator>();
builder.Services.AddMemoryCache();

var app = builder.Build();
app.MapControllers();
app.Run();
```

```csharp
// Controllers/ReportsController.cs
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportGenerator _generator;
    
    public ReportsController(IReportGenerator generator)
    {
        _generator = generator;
    }
    
    [HttpPost("generate")]
    public IActionResult GenerateReport([FromBody] ReportRequest request)
    {
        var report = _generator.Generate(
            request.ReportName,
            request.Data,
            request.Parameters,
            request.Format
        );
        
        var contentType = GetContentType(request.Format);
        return File(report, contentType, $"{request.ReportName}.{GetExtension(request.Format)}");
    }
    
    [HttpGet("invoice/{id}")]
    public IActionResult GetInvoice(int id, [FromQuery] string format = "pdf")
    {
        var invoice = GetInvoiceData(id);
        var report = _generator.GenerateInvoice(invoice, format);
        
        return File(report, GetContentType(format), $"Invoice_{id}.{format}");
    }
}
```

#### Client Implementation (Blazor WASM)

```csharp
// Client/Services/ReportClient.cs
public class ReportClient
{
    private readonly HttpClient _http;
    
    public ReportClient(HttpClient http)
    {
        _http = http;
    }
    
    public async Task<byte[]> GenerateInvoiceAsync(int invoiceId, string format = "pdf")
    {
        var response = await _http.GetAsync($"api/reports/invoice/{invoiceId}?format={format}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsByteArrayAsync();
    }
    
    public async Task<byte[]> GenerateCustomReportAsync(ReportRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/reports/generate", request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsByteArrayAsync();
    }
}
```

```razor
@* Component usage *@
@inject ReportClient ReportClient
@inject IJSRuntime JS

<button @onclick="DownloadInvoice">Download Invoice PDF</button>

@code {
    [Parameter]
    public int InvoiceId { get; set; }
    
    private async Task DownloadInvoice()
    {
        var pdf = await ReportClient.GenerateInvoiceAsync(InvoiceId, "pdf");
        await JS.InvokeVoidAsync("downloadFile", 
            $"Invoice_{InvoiceId}.pdf",
            Convert.ToBase64String(pdf),
            "application/pdf");
    }
}
```

### Advantages

✅ **Centralized management**: Single source for all reports
✅ **Independent scaling**: Scale report generation separately
✅ **Resource isolation**: Doesn't impact client applications
✅ **Code reuse**: Single implementation for all clients
✅ **Easier updates**: Update reports without redeploying clients
✅ **Works with WASM**: Supports browser-based applications

### Disadvantages

❌ **Network dependency**: Requires reliable connectivity
❌ **Additional complexity**: More moving parts to manage
❌ **Latency**: Network overhead for each request
❌ **Infrastructure costs**: Need to host API service
❌ **Not offline-capable**: Cannot generate reports offline

## Hybrid Approach

### Description

Combine both modes for maximum flexibility.

### Architecture

```
┌─────────────────────────┐
│  Desktop Application    │
│  ┌───────────────────┐  │
│  │ Embedded Reports  │  │  ◄─── Standalone Mode
│  │ (Offline capable) │  │
│  └───────────────────┘  │
│           │             │
│           │ Online?     │
│           ▼             │
│  ┌───────────────────┐  │
│  │   Report API      │  │  ◄─── API Mode
│  │   (Cloud reports) │  │
│  └───────────────────┘  │
└─────────────────────────┘
```

### Implementation

```csharp
public class HybridReportService : IReportService
{
    private readonly IReportGenerator _localGenerator;
    private readonly ReportClient _apiClient;
    private readonly IConnectivity _connectivity;
    
    public HybridReportService(
        IReportGenerator localGenerator,
        ReportClient apiClient,
        IConnectivity connectivity)
    {
        _localGenerator = localGenerator;
        _apiClient = apiClient;
        _connectivity = connectivity;
    }
    
    public async Task<byte[]> GenerateReportAsync(
        string reportName,
        object data,
        string format)
    {
        // Check if online and prefer API for complex reports
        if (_connectivity.IsConnected && IsComplexReport(reportName))
        {
            try
            {
                return await _apiClient.GenerateReportAsync(
                    new ReportRequest
                    {
                        ReportName = reportName,
                        Data = data,
                        Format = format
                    });
            }
            catch (HttpRequestException)
            {
                // Fall back to local generation
            }
        }
        
        // Generate locally
        return _localGenerator.Generate(reportName, data, null, format);
    }
    
    private bool IsComplexReport(string reportName)
    {
        // Complex reports that benefit from server-side generation
        return reportName.Contains("Consolidated") || 
               reportName.Contains("Dashboard");
    }
}
```

## Comparison Matrix

| Feature | Standalone | API | Hybrid |
|---------|-----------|-----|--------|
| **Deployment Complexity** | Low | Medium | High |
| **Scalability** | Limited | High | High |
| **Offline Support** | ✅ Yes | ❌ No | ✅ Yes |
| **Network Required** | ❌ No | ✅ Yes | Optional |
| **Resource Usage** | App Process | Dedicated | Both |
| **Code Maintenance** | Per App | Centralized | Both |
| **Blazor WASM Support** | ❌ No | ✅ Yes | ✅ Yes |
| **Update Flexibility** | Low | High | High |
| **Initial Setup Cost** | Low | Medium | High |
| **Operating Cost** | Low | Medium-High | Medium-High |

## Decision Tree

```
Start
  │
  ├─ Building Blazor WASM? ─── Yes ──► API Mode (required)
  │                      │
  │                      No
  │                      │
  ├─ Need offline reports? ─── Yes ──► Standalone or Hybrid
  │                      │
  │                      No
  │                      │
  ├─ Multiple client apps? ─── Yes ──► API Mode
  │                      │
  │                      No
  │                      │
  ├─ High report volume? ─── Yes ──► API Mode
  │                      │
  │                      No
  │                      │
  └─ Simple desktop app? ─── Yes ──► Standalone Mode
```

## Migration Strategies

### From Standalone to API

1. **Create API project** with ReportViewerCore
2. **Implement report endpoints** matching current functionality
3. **Create client library** for API access
4. **Gradually migrate** reports to API
5. **Update clients** to use API instead of local generation
6. **Remove embedded** ReportViewerCore from clients

### From API to Hybrid

1. **Add ReportViewerCore** to client application
2. **Implement local generator** service
3. **Create hybrid service** that tries API first
4. **Add connectivity checking**
5. **Implement fallback** to local generation

## Best Practices

### Standalone Mode

1. ✅ Cache compiled reports
2. ✅ Use memory-efficient rendering
3. ✅ Dispose reports properly
4. ✅ Implement error handling
5. ✅ Monitor memory usage

### API Mode

1. ✅ Implement caching (report definitions and results)
2. ✅ Use authentication and authorization
3. ✅ Add rate limiting
4. ✅ Implement request queuing for high load
5. ✅ Monitor API performance
6. ✅ Version your API
7. ✅ Document endpoints

### Hybrid Mode

1. ✅ Graceful degradation to local
2. ✅ Cache API responses
3. ✅ Sync report definitions
4. ✅ Monitor connectivity
5. ✅ Handle version mismatches

## Example: Enterprise Deployment

```csharp
// Enterprise-grade API mode with caching and monitoring
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportGenerator _generator;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ReportsController> _logger;
    private readonly IMetrics _metrics;
    
    [HttpPost("generate")]
    [RateLimit(100, TimeWindow = "1m")]
    public async Task<IActionResult> GenerateReport(
        [FromBody] ReportRequest request)
    {
        var cacheKey = $"report:{request.Hash}";
        
        // Try cache first
        var cached = await _cache.GetAsync(cacheKey);
        if (cached != null)
        {
            _metrics.IncrementCounter("reports.cache_hits");
            return File(cached, GetContentType(request.Format));
        }
        
        // Generate report
        _metrics.StartTimer("reports.generation_time");
        var report = _generator.Generate(
            request.ReportName,
            request.Data,
            request.Parameters,
            request.Format
        );
        _metrics.StopTimer("reports.generation_time");
        
        // Cache result
        await _cache.SetAsync(cacheKey, report, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        });
        
        _logger.LogInformation(
            "Generated report {ReportName} for user {User}",
            request.ReportName,
            User.Identity.Name
        );
        
        return File(report, GetContentType(request.Format));
    }
}
```

## Next Steps

- [Advanced Topics](06-Advanced-Topics.md) - Subreports, custom code, and optimization
- [Troubleshooting](07-Troubleshooting.md) - Common issues and solutions
