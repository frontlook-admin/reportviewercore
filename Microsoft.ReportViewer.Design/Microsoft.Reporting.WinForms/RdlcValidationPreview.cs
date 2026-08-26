using System.Collections;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.Reporting.WinForms;

/// <summary>Validates authoring documents without executing report code or contacting external data sources.</summary>
public sealed class RdlcDesignValidator
{
    public IReadOnlyList<RdlcDiagnostic> ValidateXml(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        try { return Validate(RdlcDocument.Load(xml)); }
        catch (RdlcDocumentFormatException exception)
        { return new[] { new RdlcDiagnostic(RdlcDiagnosticSeverity.Error, "Report", "Definition", exception.Message) }; }
    }

    public IReadOnlyList<RdlcDiagnostic> Validate(RdlcDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var authoring = new RdlcAuthoringModel(document);
        var diagnostics = authoring.ValidateReferences().ToList();
        var root = document.SourceRoot;
        foreach (var element in root.Descendants())
        {
            var expression = element.Name.LocalName is "Value" or "DataSetName" or "Hidden" or "Bookmark" ? element.Value : null;
            if (!string.IsNullOrWhiteSpace(expression) && expression.StartsWith("=", StringComparison.Ordinal))
                diagnostics.AddRange(authoring.ValidateExpression(Identity(element), element.Name.LocalName, expression));
        }
        return diagnostics.Select(x => Enrich(x, root)).ToArray();
    }

    private static RdlcDiagnostic Enrich(RdlcDiagnostic diagnostic, XElement root)
    {
        var element = root.Descendants().FirstOrDefault(x => x.Attribute("Name")?.Value == diagnostic.Item) ?? root;
        var info = (System.Xml.IXmlLineInfo)element;
        return diagnostic with { Path = diagnostic.Path ?? GetPath(element), Line = diagnostic.Line ?? (info.HasLineInfo() ? info.LineNumber : null), Column = diagnostic.Column ?? (info.HasLineInfo() ? info.LinePosition : null) };
    }

    private static string Identity(XElement element) => element.Parent?.Attribute("Name")?.Value ?? element.Parent?.Parent?.Attribute("Name")?.Value ?? element.Name.LocalName;
    private static string GetPath(XElement element) => "/" + string.Join("/", element.AncestorsAndSelf().Reverse().Select(x => x.Name.LocalName + (x.Attribute("Name") is { } name ? $"[@Name='{name.Value}']" : string.Empty)));
}

public sealed record RdlcPreviewRequest(
    RdlcDocument Document,
    IReadOnlyDictionary<string, IEnumerable<object>> SampleData,
    IReadOnlyDictionary<string, string>? Parameters = null,
    string Format = "HTML5",
    string? DeviceInfo = null,
    bool IsTrusted = false);

public sealed class RdlcPreviewResult
{
    internal RdlcPreviewResult(bool succeeded, bool stale, byte[] content, string mimeType, IReadOnlyList<RdlcDiagnostic> diagnostics)
    { Succeeded = succeeded; IsStale = stale; Content = content; MimeType = mimeType; Diagnostics = diagnostics; }
    public bool Succeeded { get; }
    public bool IsStale { get; }
    public byte[] Content { get; }
    public string MimeType { get; }
    public IReadOnlyList<RdlcDiagnostic> Diagnostics { get; }
    internal static RdlcPreviewResult Success(byte[] content, string mimeType) => new(true, false, content, mimeType, Array.Empty<RdlcDiagnostic>());
    internal static RdlcPreviewResult Failure(IEnumerable<RdlcDiagnostic> diagnostics, bool stale = false) => new(false, stale, Array.Empty<byte>(), "", diagnostics.ToArray());
    internal RdlcPreviewResult AsStale() => new(false, true, Content, MimeType, Diagnostics);
}

/// <summary>Compiles and renders a detached preview using only caller-provided in-memory data.</summary>
public sealed class RdlcPreviewService
{
    private readonly RdlcDesignValidator _validator;
    public RdlcPreviewService(RdlcDesignValidator? validator = null) => _validator = validator ?? new RdlcDesignValidator();

    public async Task<RdlcPreviewResult> RenderAsync(RdlcPreviewRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var validation = _validator.Validate(request.Document).Concat(ValidatePreviewInputs(request)).ToArray();
        if (validation.Any(x => x.Severity == RdlcDiagnosticSeverity.Error)) return RdlcPreviewResult.Failure(validation);
        try
        {
            return await Task.Run(() => RenderCore(request, cancellationToken), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return RdlcPreviewResult.Failure(new[] { new RdlcDiagnostic(RdlcDiagnosticSeverity.Info, "Preview", "Cancellation", "Preview rendering was cancelled.") }); }
        catch (Exception exception)
        { return RdlcPreviewResult.Failure(new[] { new RdlcDiagnostic(RdlcDiagnosticSeverity.Error, "Preview", "Render", exception.GetBaseException().Message) }); }
    }

    private static IEnumerable<RdlcDiagnostic> ValidatePreviewInputs(RdlcPreviewRequest request)
    {
        foreach (var dataSet in request.Document.Report.DataSets)
            if (dataSet.Name is not null && !request.SampleData.ContainsKey(dataSet.Name))
                yield return new(RdlcDiagnosticSeverity.Error, dataSet.Name, "SampleData", $"No in-memory sample data was supplied for data set '{dataSet.Name}'.");
        foreach (var parameter in request.Document.Report.Parameters)
        {
            if (parameter.Name is null || request.Parameters?.ContainsKey(parameter.Name) == true) continue;
            if (!parameter.Xml.Descendants().Any(x => x.Name.LocalName == "DefaultValue"))
                yield return new(RdlcDiagnosticSeverity.Error, parameter.Name, "Parameter", $"Preview parameter '{parameter.Name}' has no supplied value or default.");
        }
    }

    private static RdlcPreviewResult RenderCore(RdlcPreviewRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var report = new LocalReport();
        using var reader = new StringReader(PreparePreviewDefinition(request).ToString(SaveOptions.DisableFormatting));
        report.LoadReportDefinition(reader, request.IsTrusted);
        foreach (var source in request.SampleData)
        {
            cancellationToken.ThrowIfCancellationRequested();
            report.DataSources.Add(new ReportDataSource(source.Key, (IEnumerable)source.Value));
        }
        if (request.Parameters is not null)
            report.SetParameters(request.Parameters.Select(x => new ReportParameter(x.Key, x.Value)));
        var bytes = report.Render(request.Format, request.DeviceInfo, PageCountMode.Estimate, out var mimeType, out _, out _, out _, out var warnings);
        var warningDiagnostics = warnings?.Select(x => new RdlcDiagnostic(RdlcDiagnosticSeverity.Warning, "Preview", "Render", x.Message)) ?? [];
        return new RdlcPreviewResult(true, false, bytes, mimeType, warningDiagnostics.ToArray());
    }

    private static XElement PreparePreviewDefinition(RdlcPreviewRequest request)
    {
        var root = request.Document.RootXml;
        foreach (var dataSource in root.Descendants().Where(x => x.Name.LocalName == "DataSource" && x.Attribute("Name") is { } name && request.SampleData.ContainsKey(name.Value)))
        {
            var properties = dataSource.Elements().FirstOrDefault(x => x.Name.LocalName == "ConnectionProperties");
            if (properties is null)
            {
                properties = new XElement(dataSource.Name.Namespace + "ConnectionProperties");
                dataSource.Add(properties);
            }
            if (properties.Elements().All(x => x.Name.LocalName != "DataProvider")) properties.Add(new XElement(dataSource.Name.Namespace + "DataProvider", "System.Data.DataSet"));
            if (properties.Elements().All(x => x.Name.LocalName != "ConnectString")) properties.Add(new XElement(dataSource.Name.Namespace + "ConnectString", string.Empty));
        }
        return root;
    }
}

/// <summary>Owns refresh cancellation so an older preview can never replace the current edit.</summary>
public sealed class RdlcPreviewSession : IDisposable
{
    private readonly RdlcPreviewService _service;
    private readonly object _gate = new();
    private CancellationTokenSource? _refreshCancellation;
    private long _generation;
    public RdlcPreviewSession(RdlcPreviewService service) => _service = service ?? throw new ArgumentNullException(nameof(service));
    public bool IsDisposed { get; private set; }

    public async Task<RdlcPreviewResult> RefreshAsync(RdlcDocument document, IReadOnlyDictionary<string, IEnumerable<object>> sampleData, IReadOnlyDictionary<string, string>? parameters = null, string format = "HTML5", CancellationToken cancellationToken = default)
    {
        CancellationToken token;
        long generation;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
            _refreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            token = _refreshCancellation.Token;
            generation = ++_generation;
        }
        var result = await _service.RenderAsync(new RdlcPreviewRequest(document, sampleData, parameters, format), token).ConfigureAwait(false);
        lock (_gate)
        {
            if (generation != _generation || token.IsCancellationRequested) return result.AsStale();
            return result;
        }
    }

    public void Cancel()
    {
        lock (_gate) _refreshCancellation?.Cancel();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (IsDisposed) return;
            IsDisposed = true;
            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();
            _refreshCancellation = null;
        }
    }
}
