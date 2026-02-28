using Microsoft.Reporting.NETCore;

namespace Microsoft.ReportViewer.MAUI.Services;

/// <summary>
/// Service for report generation and rendering operations.
/// </summary>
public class ReportService
{
    /// <summary>
    /// Renders a report to the specified format.
    /// </summary>
    /// <param name="report">The LocalReport to render.</param>
    /// <param name="format">Output format (PDF, EXCELOPENXML, WORDOPENXML, HTML5, IMAGE).</param>
    /// <param name="deviceInfo">Optional device info XML for the renderer.</param>
    /// <returns>The rendered report as a byte array.</returns>
    public byte[] RenderReport(LocalReport report, string format, string? deviceInfo = null)
    {
        return report.Render(format, deviceInfo);
    }

    /// <summary>
    /// Renders a report to the specified format asynchronously.
    /// </summary>
    public Task<byte[]> RenderReportAsync(LocalReport report, string format, string? deviceInfo = null)
    {
        return Task.Run(() => RenderReport(report, format, deviceInfo));
    }

    /// <summary>
    /// Generates a report from the provided data and saves to file.
    /// </summary>
    /// <typeparam name="T">The type of data items.</typeparam>
    /// <param name="reportPath">Path to the RDLC file.</param>
    /// <param name="dataSourceName">Name of the data source in the report.</param>
    /// <param name="data">The data items.</param>
    /// <param name="parameters">Optional report parameters.</param>
    /// <param name="format">Output format.</param>
    /// <returns>The rendered report as a byte array.</returns>
    public byte[] GenerateReport<T>(
        string reportPath,
        string dataSourceName,
        IEnumerable<T> data,
        Dictionary<string, string>? parameters = null,
        string format = "PDF")
    {
        using var report = new LocalReport();

        using var fs = new FileStream(reportPath, FileMode.Open, FileAccess.Read);
        report.LoadReportDefinition(fs);

        report.DataSources.Add(new ReportDataSource(dataSourceName, data));

        if (parameters != null && parameters.Count > 0)
        {
            var reportParams = parameters
                .Select(p => new ReportParameter(p.Key, p.Value))
                .ToArray();
            report.SetParameters(reportParams);
        }

        return report.Render(format);
    }

    /// <summary>
    /// Generates a report from embedded resource.
    /// </summary>
    public byte[] GenerateReportFromEmbeddedResource<T>(
        string resourceName,
        System.Reflection.Assembly assembly,
        string dataSourceName,
        IEnumerable<T> data,
        Dictionary<string, string>? parameters = null,
        string format = "PDF")
    {
        using var report = new LocalReport();
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource '{resourceName}' not found in assembly '{assembly.FullName}'.");
        }

        report.LoadReportDefinition(stream);
        report.DataSources.Add(new ReportDataSource(dataSourceName, data));

        if (parameters != null && parameters.Count > 0)
        {
            var reportParams = parameters
                .Select(p => new ReportParameter(p.Key, p.Value))
                .ToArray();
            report.SetParameters(reportParams);
        }

        return report.Render(format);
    }

    /// <summary>
    /// Generates a report from a stream.
    /// </summary>
    public byte[] GenerateReportFromStream<T>(
        Stream reportStream,
        string dataSourceName,
        IEnumerable<T> data,
        Dictionary<string, string>? parameters = null,
        string format = "PDF")
    {
        using var report = new LocalReport();
        report.LoadReportDefinition(reportStream);

        report.DataSources.Add(new ReportDataSource(dataSourceName, data));

        if (parameters != null && parameters.Count > 0)
        {
            var reportParams = parameters
                .Select(p => new ReportParameter(p.Key, p.Value))
                .ToArray();
            report.SetParameters(reportParams);
        }

        return report.Render(format);
    }

    /// <summary>
    /// Saves report data to a file.
    /// </summary>
    public async Task<string> SaveToFileAsync(byte[] data, string fileName, string? folder = null)
    {
        folder ??= FileSystem.CacheDirectory;
        var filePath = Path.Combine(folder, fileName);
        
        await File.WriteAllBytesAsync(filePath, data);
        
        return filePath;
    }

    /// <summary>
    /// Shares a report file using the system share dialog.
    /// </summary>
    public async Task ShareReportAsync(byte[] data, string fileName, string? title = null)
    {
        var filePath = await SaveToFileAsync(data, fileName);
        
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title ?? "Share Report",
            File = new ShareFile(filePath)
        });
    }

    /// <summary>
    /// Opens a report file using the default system application.
    /// </summary>
    public async Task OpenReportAsync(byte[] data, string fileName)
    {
        var filePath = await SaveToFileAsync(data, fileName);
        
        await Launcher.Default.OpenAsync(new OpenFileRequest
        {
            File = new ReadOnlyFile(filePath)
        });
    }

    /// <summary>
    /// Gets the available export formats.
    /// </summary>
    public static IReadOnlyList<ExportFormat> GetAvailableFormats()
    {
        return new List<ExportFormat>
        {
            new("PDF", "PDF", ".pdf", "Adobe PDF Document"),
            new("EXCELOPENXML", "Excel", ".xlsx", "Microsoft Excel Workbook"),
            new("WORDOPENXML", "Word", ".docx", "Microsoft Word Document"),
            new("HTML5", "HTML", ".html", "HTML Web Page"),
            new("IMAGE", "Image", ".png", "PNG Image")
        };
    }
}

/// <summary>
/// Represents an export format option.
/// </summary>
public record ExportFormat(string Format, string DisplayName, string Extension, string Description);
