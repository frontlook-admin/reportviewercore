using System.Xml;
using System.Xml.Linq;

namespace Microsoft.Reporting.WinForms;

/// <summary>Lossless, preservation-oriented representation of an RDLC document.</summary>
public sealed class RdlcDocument
{
    private readonly XDocument _xml;

    private RdlcDocument(XDocument xml)
    {
        _xml = xml;
        Report = new RdlcReport(xml.Root!);
    }

    public RdlcReport Report { get; private set; }

    /// <summary>Returns a detached XML tree for advanced/extension editing.</summary>
    public XElement RootXml => new XElement(_xml.Root!);

    public static RdlcDocument Load(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            return Parse(XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo));
        }
        catch (RdlcDocumentFormatException) { throw; }
        catch (XmlException exception)
        {
            throw new RdlcDocumentFormatException("The RDLC contains malformed XML.", exception);
        }
    }

    public static RdlcDocument Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            return Parse(XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo));
        }
        catch (RdlcDocumentFormatException) { throw; }
        catch (XmlException exception)
        {
            throw new RdlcDocumentFormatException("The RDLC contains malformed XML.", exception);
        }
    }

    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { OmitXmlDeclaration = _xml.Declaration is null, Indent = false });
        _xml.Save(writer);
    }

    public string ToXml() => _xml.ToString(SaveOptions.DisableFormatting);

    internal XElement MutableRoot => _xml.Root!;
    internal XElement SourceRoot => _xml.Root!;
    internal void Refresh() => Report = new RdlcReport(_xml.Root!);

    private static RdlcDocument Parse(XDocument xml)
    {
        if (xml.Root is null || xml.Root.Name.LocalName != "Report")
            throw new RdlcDocumentFormatException("Unsupported RDLC structure: the required Report root element is missing.");
        if (xml.Root.Elements().All(x => x.Name.LocalName != "ReportSections"))
            throw new RdlcDocumentFormatException("Unsupported RDLC structure: the required ReportSections element is missing.");
        return new RdlcDocument(xml);
    }
}

public sealed class RdlcReport
{
    private readonly XElement _element;
    internal RdlcReport(XElement element)
    {
        _element = element;
        DataSources = Children("DataSources", "DataSource").Select(x => new RdlcDataSource(x)).ToArray();
        DataSets = Children("DataSets", "DataSet").Select(x => new RdlcDataSet(x)).ToArray();
        Parameters = Children("ReportParameters", "ReportParameter").Select(x => new RdlcReportParameter(x)).ToArray();
        Sections = Children("ReportSections", "ReportSection").Select(x => new RdlcReportSection(x)).ToArray();
    }

    public string? Name => _element.Attribute("Name")?.Value;
    public string NamespaceUri => _element.Name.NamespaceName;
    public IReadOnlyList<RdlcDataSource> DataSources { get; }
    public IReadOnlyList<RdlcDataSet> DataSets { get; }
    public IReadOnlyList<RdlcReportParameter> Parameters { get; }
    public IReadOnlyList<RdlcReportSection> Sections { get; }
    public IReadOnlyList<XElement> Extensions => _element.Elements().Where(x => !Known.Contains(x.Name.LocalName)).Select(x => new XElement(x)).ToArray();
    internal string? GetProperty(string name) => name == "Name" ? Name : throw new ArgumentException($"Unknown report property '{name}'.", nameof(name));
    internal void SetProperty(string name, object? value)
    {
        if (name != "Name") throw new ArgumentException($"Unknown report property '{name}'.", nameof(name));
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text)) throw new FormatException("Report Name cannot be empty.");
        _element.SetAttributeValue("Name", text);
    }

    private IEnumerable<XElement> Children(string container, string child) =>
        _element.Elements().FirstOrDefault(x => x.Name.LocalName == container)?.Elements().Where(x => x.Name.LocalName == child) ?? [];

    private static readonly HashSet<string> Known = new(StringComparer.Ordinal) { "AutoRefresh", "DataSources", "DataSets", "ReportSections", "ReportParameters", "ReportParametersLayout", "Code", "EmbeddedImages", "Language", "ConsumeContainerWhitespace", "Variables", "CustomProperties", "ReportUnitType", "ReportView", "Description", "Author", "InitialPageName" };
}

public sealed class RdlcReportSection
{
    private readonly XElement _element;
    internal RdlcReportSection(XElement element) { _element = element; Body = Child("Body") is { } body ? new RdlcBody(body) : null; Page = Child("Page") is { } page ? new RdlcPage(page) : null; }
    public RdlcBody? Body { get; }
    public RdlcPage? Page { get; }
    public IReadOnlyList<XElement> Extensions => _element.Elements().Where(x => x.Name.LocalName is not ("Body" or "Width" or "Page")).Select(x => new XElement(x)).ToArray();
    private XElement? Child(string name) => _element.Elements().FirstOrDefault(x => x.Name.LocalName == name);
}

public sealed class RdlcPage
{
    private readonly XElement _element;
    internal RdlcPage(XElement element) => _element = element;
    public string? PageWidth => Child("PageWidth")?.Value;
    public string? PageHeight => Child("PageHeight")?.Value;
    public IReadOnlyList<XElement> Extensions => _element.Elements().Where(x => x.Name.LocalName is not ("PageWidth" or "PageHeight" or "LeftMargin" or "RightMargin" or "TopMargin" or "BottomMargin" or "Style")).Select(x => new XElement(x)).ToArray();
    internal string? GetProperty(string name) => name switch { "PageWidth" => PageWidth, "PageHeight" => PageHeight, _ => throw new ArgumentException($"Unknown page property '{name}'.", nameof(name)) };
    internal void SetProperty(string name, object? value)
    {
        if (name is not ("PageWidth" or "PageHeight")) throw new ArgumentException($"Unknown page property '{name}'.", nameof(name));
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text) || !System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+(\.\d+)?(in|cm|mm|pt|px)$")) throw new FormatException($"{name} must be a positive report size with a supported unit.");
        var child = Child(name); if (child is null) _element.Add(new XElement(_element.Name.Namespace + name, text)); else child.Value = text;
    }
    private XElement? Child(string name) => _element.Elements().FirstOrDefault(x => x.Name.LocalName == name);
}

public sealed class RdlcBody
{
    private readonly XElement _element;
    internal RdlcBody(XElement element) { _element = element; }
    public string? Height => Child("Height")?.Value;
    public IReadOnlyList<RdlcReportItem> ReportItems => Child("ReportItems")?.Elements().Select(x => new RdlcReportItem(x)).ToArray() ?? [];
    internal XElement ItemsElement => Child("ReportItems") ?? throw new InvalidOperationException("The report body has no ReportItems element.");
    public IReadOnlyList<XElement> Extensions => _element.Elements().Where(x => x.Name.LocalName is not ("ReportItems" or "Height" or "Style" or "Width")).Select(x => new XElement(x)).ToArray();
    private XElement? Child(string name) => _element.Elements().FirstOrDefault(x => x.Name.LocalName == name);
}

public sealed class RdlcReportItem
{
    private readonly XElement _element;
    internal RdlcReportItem(XElement element) => _element = element;
    public string ElementName => _element.Name.LocalName;
    public string? Name => _element.Attribute("Name")?.Value;
    public string NamespaceUri => _element.Name.NamespaceName;
    public XElement Xml => new(_element);
    internal string? GetProperty(string name) => name switch { "Name" => Name, "Left" or "Top" or "Width" or "Height" => _element.Attribute(name)?.Value, _ => throw new ArgumentException($"Unknown report-item property '{name}'.", nameof(name)) };
    internal void SetProperty(string name, object? value)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text)) throw new FormatException($"{name} cannot be empty.");
        if (name is "Left" or "Top" or "Width" or "Height" && !System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+(\.\d+)?(in|cm|mm|pt|px)$")) throw new FormatException($"{name} must be a report size with a supported unit.");
        if (name is not ("Name" or "Left" or "Top" or "Width" or "Height")) throw new ArgumentException($"Unknown report-item property '{name}'.", nameof(name));
        _element.SetAttributeValue(name, text);
    }
}

public sealed class RdlcDataSource
{
    private readonly XElement _element;
    internal RdlcDataSource(XElement element) => _element = element;
    public string? Name => _element.Attribute("Name")?.Value;
    public XElement Xml => new(_element);
    internal void SetName(string name) => _element.SetAttributeValue("Name", name);
    internal void Remove() => _element.Remove();
}

public sealed class RdlcDataSet
{
    private readonly XElement _element;
    internal RdlcDataSet(XElement element) => _element = element;
    public string? Name => _element.Attribute("Name")?.Value;
    public string? DataSourceName => _element.Elements().FirstOrDefault(x => x.Name.LocalName == "Query")?.Elements().FirstOrDefault(x => x.Name.LocalName == "DataSourceName")?.Value;
    public IReadOnlyList<RdlcDataSetField> Fields => _element.Descendants().Where(x => x.Name.LocalName == "Field").Select(x => new RdlcDataSetField(x.Attribute("Name")?.Value ?? "", x.Elements().FirstOrDefault(y => y.Name.LocalName == "DataType")?.Value)).ToArray();
    public XElement Xml => new(_element);
    internal void SetName(string name) => _element.SetAttributeValue("Name", name);
    internal void Remove() => _element.Remove();
}

public sealed class RdlcReportParameter
{
    private readonly XElement _element;
    internal RdlcReportParameter(XElement element) => _element = element;
    public string? Name => _element.Attribute("Name")?.Value;
    public string? DataType => _element.Elements().FirstOrDefault(x => x.Name.LocalName == "DataType")?.Value;
    public XElement Xml => new(_element);
    internal void SetName(string name) => _element.SetAttributeValue("Name", name);
    internal void Remove() => _element.Remove();
}

public sealed class RdlcDocumentFormatException : FormatException
{
    public RdlcDocumentFormatException(string message) : base(message) { }
    public RdlcDocumentFormatException(string message, Exception innerException) : base(message, innerException) { }
}
