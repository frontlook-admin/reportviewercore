#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reporting.WinForms;

namespace Microsoft.Reporting.NETCore;

/// <summary>One named in-memory DataTable supplied to a headless report render.</summary>
public sealed record HeadlessReportDataSource
{
    public HeadlessReportDataSource(string name, DataTable data)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A data source name is required.", nameof(name));
        Name = name;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public string Name { get; }
    public DataTable Data { get; }
}

/// <summary>Definition and data sources for one named RDLC subreport.</summary>
public sealed record HeadlessSubreportDefinition
{
    public HeadlessSubreportDefinition(string name, byte[] definition, IEnumerable<HeadlessReportDataSource>? dataSources = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A subreport name is required.", nameof(name));
        Name = name;
        Definition = (definition ?? throw new ArgumentNullException(nameof(definition))).ToArray();
        DataSources = new ReadOnlyCollection<HeadlessReportDataSource>((dataSources ?? Array.Empty<HeadlessReportDataSource>()).ToList());
    }

    public string Name { get; }
    public byte[] Definition { get; }
    public IReadOnlyList<HeadlessReportDataSource> DataSources { get; }
}

/// <summary>Immutable input snapshot for a headless LocalReport export.</summary>
public sealed record HeadlessReportRequest
{
    public HeadlessReportRequest(
        byte[] definition,
        IEnumerable<HeadlessReportDataSource>? dataSources = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? parameters = null,
        IEnumerable<HeadlessSubreportDefinition>? subreports = null,
        string format = "PDF",
        string? deviceInfo = null,
        CancellationToken cancellationToken = default,
        ReportSecurityPolicy? securityPolicy = null,
        bool isTrusted = false)
    {
        Definition = (definition ?? throw new ArgumentNullException(nameof(definition))).ToArray();
        DataSources = new ReadOnlyCollection<HeadlessReportDataSource>((dataSources ?? Array.Empty<HeadlessReportDataSource>()).ToList());
        Parameters = new ReadOnlyDictionary<string, IReadOnlyList<string>>(
            (parameters ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase))
                .ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
        Subreports = new ReadOnlyCollection<HeadlessSubreportDefinition>((subreports ?? Array.Empty<HeadlessSubreportDefinition>()).ToList());
        Format = string.IsNullOrWhiteSpace(format) ? throw new ArgumentException("A rendering format is required.", nameof(format)) : format;
        DeviceInfo = deviceInfo;
        CancellationToken = cancellationToken;
        SecurityPolicy = securityPolicy;
        IsTrusted = isTrusted;
    }

    public byte[] Definition { get; }
    public IReadOnlyList<HeadlessReportDataSource> DataSources { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Parameters { get; }
    public IReadOnlyList<HeadlessSubreportDefinition> Subreports { get; init; }
    public string Format { get; init; }
    public string? DeviceInfo { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public ReportSecurityPolicy? SecurityPolicy { get; init; }
    public bool IsTrusted { get; init; }
}

/// <summary>One stream produced by a renderer, including secondary resource streams.</summary>
public sealed record HeadlessReportStream(string Name, string Extension, string MimeType, string Encoding, byte[] Content);

/// <summary>Immutable result of a headless export.</summary>
public sealed record HeadlessReportResult(
    string Format,
    string MimeType,
    string Encoding,
    string Extension,
    byte[] Content,
    IReadOnlyList<HeadlessReportStream> Streams,
    IReadOnlyList<Warning> Warnings);

/// <summary>Creates isolated LocalReport instances for headless exports. This is not a security boundary.</summary>
public static class HeadlessReportRenderer
{
    /// <summary>Maps CLI aliases to the renderer's canonical format names.</summary>
    public static string GetRendererFormatName(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("A rendering format is required.", nameof(format));

        return format.Trim().ToUpperInvariant() switch
        {
            "HTML4_0" or "HTML4.0" => "HTML4.0",
            var canonical => canonical
        };
    }

    /// <summary>Returns the conventional file extension for a renderer output.</summary>
    public static string GetFileExtension(string format, string rendererExtension)
    {
        if (string.Equals(GetRendererFormatName(format), "HTML4.0", StringComparison.OrdinalIgnoreCase))
            return "html";
        return string.IsNullOrWhiteSpace(rendererExtension) ? format.Trim().ToLowerInvariant() : rendererExtension;
    }

    public static HeadlessReportResult Render(HeadlessReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.CancellationToken.ThrowIfCancellationRequested();

        using var report = CreateReport(request);

        var streams = new List<OutputCapture>();
        report.Render(GetRendererFormatName(request.Format), request.DeviceInfo ?? string.Empty,
            (name, extension, encoding, mimeType, willSeek) =>
            {
                request.CancellationToken.ThrowIfCancellationRequested();
                var stream = new MemoryStream();
                streams.Add(new OutputCapture(name, extension, mimeType, encoding?.WebName ?? string.Empty, stream));
                return stream;
            }, out var warnings);

        request.CancellationToken.ThrowIfCancellationRequested();
        if (streams.Count == 0)
            throw new InvalidOperationException("The renderer did not produce an output stream.");

        var outputs = streams.Select(stream => new HeadlessReportStream(stream.Name, stream.Extension, stream.MimeType, stream.Encoding, stream.Content.ToArray())).ToArray();
        var primary = outputs[0];
        return new HeadlessReportResult(request.Format, primary.MimeType, primary.Encoding, primary.Extension,
            primary.Content.ToArray(), new ReadOnlyCollection<HeadlessReportStream>(outputs),
            new ReadOnlyCollection<Warning>(warnings ?? Array.Empty<Warning>()));
    }

    /// <summary>Validates a report definition and its supplied inputs without rendering output.</summary>
    public static void Validate(HeadlessReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.CancellationToken.ThrowIfCancellationRequested();

        using var report = CreateReport(request);
        _ = report.GetTotalPages(out _);
        request.CancellationToken.ThrowIfCancellationRequested();
    }

    public static Task<HeadlessReportResult> RenderAsync(HeadlessReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Render(request), request.CancellationToken);
    }

    private static LocalReport CreateReport(HeadlessReportRequest request)
    {
        var report = new LocalReport();
        try
        {
            using var definition = new MemoryStream(request.Definition, writable: false);
            if (request.SecurityPolicy != null)
                report.SecurityPolicy = request.SecurityPolicy;
            report.LoadReportDefinition(new StreamReader(definition), request.IsTrusted);

            foreach (var source in request.DataSources)
                report.DataSources.Add(new ReportDataSource(source.Name, source.Data));

            if (request.Parameters.Count != 0)
            {
                report.SetParameters(request.Parameters.Select(parameter =>
                    new ReportParameter(parameter.Key, parameter.Value.ToArray())));
            }

            foreach (var subreport in request.Subreports)
            {
                using var subreportDefinition = new MemoryStream(subreport.Definition, writable: false);
                report.LoadSubreportDefinition(subreport.Name, new StreamReader(subreportDefinition), request.IsTrusted);
            }

            if (request.Subreports.Any(subreport => subreport.DataSources.Count != 0))
            {
                report.SubreportProcessing += (_, args) =>
                {
                    var definitionForName = request.Subreports.FirstOrDefault(item =>
                        string.Equals(item.Name, args.ReportPath, StringComparison.OrdinalIgnoreCase));
                    if (definitionForName != null)
                    {
                        foreach (var source in definitionForName.DataSources)
                            args.DataSources.Add(new ReportDataSource(source.Name, source.Data));
                    }
                };
            }

            return report;
        }
        catch
        {
            report.Dispose();
            throw;
        }
    }

    private sealed record OutputCapture(string Name, string Extension, string MimeType, string Encoding, MemoryStream Content);
}
