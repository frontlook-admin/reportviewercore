using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.Reporting.WinForms;

public enum RdlcDiagnosticSeverity { Info, Warning, Error }

public sealed record RdlcDiagnostic(RdlcDiagnosticSeverity Severity, string Item, string Property, string Message, string? Path = null, int? Line = null, int? Column = null)
{
    public override string ToString() => $"{Path ?? Item}.{Property}: {Message}" + (Line is { } line ? $" (line {line}{(Column is { } column ? $", column {column}" : string.Empty)})" : string.Empty);
}

/// <summary>Authoring operations that preserve the original RDLC tree and unknown elements.</summary>
public sealed class RdlcAuthoringModel
{
    private readonly RdlcDocument _document;
    public RdlcAuthoringModel(RdlcDocument document) => _document = document ?? throw new ArgumentNullException(nameof(document));

    public RdlcDataSource AddDataSource(string name, string? provider = null)
    {
        ValidateName(name, "DataSource");
        EnsureUnique(_document.Report.DataSources.Select(x => x.Name), name, "DataSource");
        var container = EnsureContainer("DataSources");
        var element = new XElement(Ns + "DataSource", new XAttribute("Name", name));
        if (!string.IsNullOrWhiteSpace(provider)) element.Add(new XElement(Ns + "ConnectionProperties", new XElement(Ns + "DataProvider", provider)));
        container.Add(element); Refresh(); return _document.Report.DataSources.Single(x => x.Name == name);
    }

    public void RenameDataSource(string oldName, string newName)
    {
        var source = Find(_document.Report.DataSources, oldName, "DataSource"); ValidateName(newName, "DataSource");
        EnsureUnique(_document.Report.DataSources.Where(x => x.Name != oldName).Select(x => x.Name), newName, "DataSource");
        source.SetName(newName);
        foreach (var query in Descendants("Query")) SetValue(query, "DataSourceName", oldName, newName);
        Refresh();
    }

    public void DeleteDataSource(string name, bool requireNoReferences = true)
    {
        var source = Find(_document.Report.DataSources, name, "DataSource");
        var references = Descendants("DataSourceName").Where(x => x.Value == name).ToArray();
        if (requireNoReferences && references.Length != 0) throw new RdlcAuthoringException($"DataSource '{name}' is referenced by a dataset query.");
        source.Remove(); Refresh();
    }

    public RdlcDataSet AddDataSet(string name, string dataSourceName, string? commandText = null)
    {
        ValidateName(name, "DataSet"); EnsureUnique(_document.Report.DataSets.Select(x => x.Name), name, "DataSet");
        Find(_document.Report.DataSources, dataSourceName, "DataSource");
        var query = new XElement(Ns + "Query", new XElement(Ns + "DataSourceName", dataSourceName));
        if (commandText is not null) query.Add(new XElement(Ns + "CommandText", commandText));
        EnsureContainer("DataSets").Add(new XElement(Ns + "DataSet", new XAttribute("Name", name), query)); Refresh();
        return _document.Report.DataSets.Single(x => x.Name == name);
    }

    public void RenameDataSet(string oldName, string newName)
    {
        var set = Find(_document.Report.DataSets, oldName, "DataSet"); ValidateName(newName, "DataSet");
        EnsureUnique(_document.Report.DataSets.Where(x => x.Name != oldName).Select(x => x.Name), newName, "DataSet");
        set.SetName(newName);
        foreach (var item in Descendants().Where(x => x.Name.LocalName == "DataSetName" && x.Value == oldName)) item.Value = newName;
        foreach (var item in Descendants().Where(x => x.Name.LocalName == "DataSet" && x.Attribute("Name")?.Value == oldName)) item.SetAttributeValue("Name", newName);
        Refresh();
    }

    public void DeleteDataSet(string name, bool requireNoReferences = true)
    {
        var set = Find(_document.Report.DataSets, name, "DataSet");
        var refs = Descendants().Where(x => x.Name.LocalName == "DataSetName" && x.Value == name).ToArray();
        if (requireNoReferences && refs.Length != 0) throw new RdlcAuthoringException($"DataSet '{name}' is referenced by {refs[0].Parent?.Parent?.Name.LocalName ?? "an item"}.DataSetName.");
        set.Remove(); Refresh();
    }

    public RdlcReportParameter AddParameter(string name, string dataType = "String", string? defaultValue = null, bool hidden = false)
    {
        ValidateName(name, "ReportParameter"); EnsureUnique(_document.Report.Parameters.Select(x => x.Name), name, "ReportParameter");
        if (!new[] { "Boolean", "DateTime", "Float", "Integer", "String" }.Contains(dataType, StringComparer.Ordinal)) throw new RdlcAuthoringException($"ReportParameter.DataType: unsupported type '{dataType}'.");
        var children = new List<XElement> { new(Ns + "DataType", dataType) };
        if (defaultValue is not null) children.Add(new XElement(Ns + "DefaultValue", new XElement(Ns + "Values", new XElement(Ns + "Value", defaultValue))));
        children.Add(new XElement(Ns + "Prompt", name));
        if (hidden) children.Add(new XElement(Ns + "Hidden", "true"));
        EnsureContainer("ReportParameters").Add(new XElement(Ns + "ReportParameter", new XAttribute("Name", name), children));
        EnsureParameterLayoutCell(); Refresh();
        return _document.Report.Parameters.Single(x => x.Name == name);
    }

    public void SetParameterVisibility(string name, bool hidden)
    {
        var parameter = Find(_document.Report.Parameters, name, "ReportParameter");
        var element = parameter.Xml; // detached view; locate the authoritative element below.
        var actual = _document.MutableRoot.Descendants().First(x => x.Name.LocalName == "ReportParameter" && x.Attribute("Name")?.Value == name);
        var hiddenElement = actual.Elements().FirstOrDefault(x => x.Name.LocalName == "Hidden");
        if (hiddenElement is null) actual.Add(new XElement(Ns + "Hidden", hidden ? "true" : "false")); else hiddenElement.Value = hidden ? "true" : "false";
        Refresh();
    }

    public void AddDataSetField(string dataSetName, string fieldName, string? dataType = null, string? dataField = null)
    {
        ValidateName(fieldName, "Field");
        var set = Find(_document.Report.DataSets, dataSetName, "DataSet");
        if (set.Fields.Any(x => x.Name == fieldName)) throw new RdlcAuthoringException($"DataSet '{dataSetName}'.Field: '{fieldName}' already exists.");
        var field = new XElement(Ns + "Field", new XAttribute("Name", fieldName));
        if (!string.IsNullOrWhiteSpace(dataField)) field.Add(new XElement(Ns + "DataField", dataField));
        if (!string.IsNullOrWhiteSpace(dataType)) field.Add(new XElement(Ns + "rd", new XElement(Ns + "DataType", dataType)));
        _document.MutableRoot.Descendants().First(x => x.Name.LocalName == "DataSet" && x.Attribute("Name")?.Value == dataSetName).Add(field);
        Refresh();
    }

    public void AddGrouping(string itemName, string groupName, string expression)
        => AddExpressionChild(itemName, "Grouping", "Group", groupName, expression);

    public void AddSort(string itemName, string expression, string direction = "Ascending")
    {
        if (direction is not ("Ascending" or "Descending")) throw new RdlcAuthoringException("SortExpression.Direction: expected Ascending or Descending.");
        var item = FindItem(itemName); var sort = item.Elements().FirstOrDefault(x => x.Name.LocalName == "SortExpressions") ?? new XElement(Ns + "SortExpressions");
        if (sort.Parent is null) item.Add(sort);
        sort.Add(new XElement(Ns + "SortExpression", new XElement(Ns + "Value", expression), new XElement(Ns + "Direction", direction)));
    }

    public void AddFilter(string itemName, string expression, string @operator, string value)
    {
        if (string.IsNullOrWhiteSpace(@operator)) throw new RdlcAuthoringException("Filter.Operator: cannot be empty.");
        var item = FindItem(itemName); var filters = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Filters") ?? new XElement(Ns + "Filters");
        if (filters.Parent is null) item.Add(filters);
        filters.Add(new XElement(Ns + "Filter", new XElement(Ns + "FilterExpression", expression), new XElement(Ns + "Operator", @operator), new XElement(Ns + "FilterValues", new XElement(Ns + "FilterValue", value))));
    }

    public void RenameParameter(string oldName, string newName)
    {
        var parameter = Find(_document.Report.Parameters, oldName, "ReportParameter"); ValidateName(newName, "ReportParameter");
        EnsureUnique(_document.Report.Parameters.Where(x => x.Name != oldName).Select(x => x.Name), newName, "ReportParameter");
        parameter.SetName(newName);
        foreach (var expression in Descendants().Where(x => x.Name.LocalName == "Value" || x.Name.LocalName == "DefaultValue"))
            expression.Value = Regex.Replace(expression.Value, $@"(?<![A-Za-z0-9_])Parameters!{Regex.Escape(oldName)}(?=[!.])", $"Parameters!{newName}");
        Refresh();
    }

    public void DeleteParameter(string name, bool requireNoReferences = true)
    {
        var parameter = Find(_document.Report.Parameters, name, "ReportParameter");
        var refs = Descendants().Where(x => Regex.IsMatch(x.Value, $@"Parameters!{Regex.Escape(name)}[!.]", RegexOptions.CultureInvariant)).ToArray();
        if (requireNoReferences && refs.Length != 0) throw new RdlcAuthoringException($"ReportParameter '{name}' is referenced by an expression.");
        parameter.Remove(); Refresh();
    }

    public IReadOnlyList<RdlcDiagnostic> ValidateReferences()
    {
        var result = new List<RdlcDiagnostic>();
        var sources = _document.Report.DataSources.Select(x => x.Name).Where(x => x is not null).ToHashSet(StringComparer.Ordinal);
        var sets = _document.Report.DataSets.Select(x => x.Name).Where(x => x is not null).ToHashSet(StringComparer.Ordinal);
        foreach (var item in Descendants("DataSourceName").Where(x => !sources.Contains(x.Value))) result.Add(new(RdlcDiagnosticSeverity.Error, item.AncestorsAndSelf().FirstOrDefault(x => x.Attribute("Name") is not null)?.Attribute("Name")?.Value ?? "Query", "DataSourceName", $"Data source '{item.Value}' does not exist."));
        foreach (var item in Descendants().Where(x => x.Name.LocalName == "DataSetName" && !sets.Contains(x.Value))) result.Add(new(RdlcDiagnosticSeverity.Error, item.AncestorsAndSelf().FirstOrDefault(x => x.Attribute("Name") is not null)?.Attribute("Name")?.Value ?? "ReportItem", "DataSetName", $"Data set '{item.Value}' does not exist."));
        return result;
    }

    public IReadOnlyList<RdlcDiagnostic> ValidateExpression(string item, string property, string expression)
    {
        var result = new List<RdlcDiagnostic>();
        if (string.IsNullOrWhiteSpace(expression)) result.Add(new(RdlcDiagnosticSeverity.Error, item, property, "Expression cannot be empty."));
        else if (!expression.StartsWith("=", StringComparison.Ordinal)) result.Add(new(RdlcDiagnosticSeverity.Error, item, property, "RDLC expressions must start with '='."));
        else if (expression.Count(c => c == '"') % 2 != 0) result.Add(new(RdlcDiagnosticSeverity.Error, item, property, "Expression contains an unterminated string literal."));
        foreach (Match match in Regex.Matches(expression, @"Fields!([A-Za-z_][A-Za-z0-9_]*)(?=[!.])")) if (!_document.Report.DataSets.SelectMany(x => x.Fields).Any(x => x.Name == match.Groups[1].Value)) result.Add(new(RdlcDiagnosticSeverity.Warning, item, property, $"Field '{match.Groups[1].Value}' is not declared by a dataset."));
        foreach (Match match in Regex.Matches(expression, @"Parameters!([A-Za-z_][A-Za-z0-9_]*)(?=[!.])")) if (!_document.Report.Parameters.Any(x => x.Name == match.Groups[1].Value)) result.Add(new(RdlcDiagnosticSeverity.Error, item, property, $"Parameter '{match.Groups[1].Value}' does not exist."));
        return result;
    }

    public IReadOnlyList<string> CompleteExpression(string expression, int position)
    {
        var prefix = expression[..Math.Clamp(position, 0, expression.Length)];
        if (prefix.EndsWith("Fields!", StringComparison.Ordinal)) return _document.Report.DataSets.SelectMany(x => x.Fields).Select(x => x.Name!).Where(x => x is not null).Distinct(StringComparer.Ordinal).OrderBy(x => x).ToArray();
        if (prefix.EndsWith("Parameters!", StringComparison.Ordinal)) return _document.Report.Parameters.Select(x => x.Name!).Where(x => x is not null).OrderBy(x => x).ToArray();
        return [];
    }

    public RdlcReportItem AddTextBox(string name, string value, string left, string top, string width, string height, string? containerName = null)
        => AddItem("Textbox", name, left, top, width, height, containerName, TextContent(value));

    public RdlcReportItem AddTextBox(string name, string value)
        => AddTextBox(name, value, "0in", "0in", "2in", ".25in");

    public void AddTextRun(string textBoxName, string value, string? fontWeight = null)
    {
        var textBox = FindItem(textBoxName);
        if (textBox.Name.LocalName != "Textbox") throw new RdlcAuthoringException($"ReportItem '{textBoxName}' is not a textbox.");
        var runs = textBox.Descendants().FirstOrDefault(x => x.Name.LocalName == "TextRuns") ?? throw new RdlcAuthoringException($"Textbox '{textBoxName}' has no text paragraph.");
        var run = new XElement(Ns + "TextRun", new XElement(Ns + "Value", value));
        if (fontWeight is not null) run.Add(new XElement(Ns + "Style", new XElement(Ns + "FontWeight", fontWeight)));
        runs.Add(run);
    }

    public RdlcReportItem AddRectangle(string name, string left, string top, string width, string height, string? containerName = null)
        => AddItem("Rectangle", name, left, top, width, height, containerName);

    public RdlcReportItem AddImage(string name, string source, string left, string top, string width, string height, string? containerName = null)
        => AddItem("Image", name, left, top, width, height, containerName, new XElement(Ns + "Source", source));

    public RdlcReportItem AddLine(string name, string left, string top, string width, string height, string? containerName = null)
        => AddItem("Line", name, left, top, width, height, containerName);

    public RdlcReportItem InsertToolboxItem(string kind, string name, string? value = null, string? containerName = null)
        => kind switch
        {
            "Textbox" => AddTextBox(name, value ?? string.Empty, "0in", "0in", "2in", ".25in", containerName),
            "Rectangle" => AddRectangle(name, "0in", "0in", "2in", "1in", containerName),
            "Image" => AddImage(name, value ?? string.Empty, "0in", "0in", "1in", "1in", containerName),
            "Line" => AddLine(name, "0in", "0in", "2in", ".02in", containerName),
            "Chart" => AddChart(name, value ?? throw new RdlcAuthoringException("Toolbox Chart requires a dataset name."), "0in", "0in", "5in", "3in", containerName),
            "Subreport" => AddSubreport(name, value ?? throw new RdlcAuthoringException("Toolbox Subreport requires a report name."), "0in", "0in", "5in", "2in", containerName),
            _ => throw new RdlcAuthoringException($"Toolbox: unsupported report item '{kind}'.")
        };

    public RdlcReportItem AddPageHeaderTextBox(string name, string value, string left, string top, string width, string height)
        => AddSectionTextBox("PageHeader", name, value, left, top, width, height);

    public RdlcReportItem AddPageFooterTextBox(string name, string value, string left, string top, string width, string height)
        => AddSectionTextBox("PageFooter", name, value, left, top, width, height);

    public void DeleteReportItem(string name)
    {
        var item = FindItem(name);
        item.Remove();
        Refresh();
    }

    public void SetItemFormatting(string itemName, string property, string value)
    {
        if (property is not ("BackgroundColor" or "Color" or "FontFamily" or "FontSize" or "FontWeight" or "TextAlign" or "VerticalAlign" or "Format" or "BorderStyle" or "BorderColor"))
            throw new RdlcAuthoringException($"ReportItem.Style: unsupported property '{property}'.");
        if (string.IsNullOrWhiteSpace(value)) throw new FormatException($"{property} cannot be empty.");
        var item = FindItem(itemName);
        var style = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Style");
        if (style is null) { style = new XElement(Ns + "Style"); item.Add(style); }
        var element = style.Elements().FirstOrDefault(x => x.Name.LocalName == property);
        if (element is null) style.Add(new XElement(Ns + property, value)); else element.Value = value;
    }

    public void SetItemVisibility(string itemName, string? hiddenExpression)
    {
        var item = FindItem(itemName);
        var hidden = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Hidden");
        if (hiddenExpression is null) { hidden?.Remove(); return; }
        if (string.IsNullOrWhiteSpace(hiddenExpression)) throw new FormatException("Hidden cannot be empty.");
        if (hidden is null) item.Add(new XElement(Ns + "Hidden", hiddenExpression)); else hidden.Value = hiddenExpression;
    }

    public RdlcReportItem AddTablix(string name, string dataSetName, string left, string top, string width, string height, string? containerName = null)
    {
        Find(_document.Report.DataSets, dataSetName, "DataSet");
        var item = AddItem("Tablix", name, left, top, width, height, containerName, new XElement(Ns + "DataSetName", dataSetName));
        var actual = FindItem(name);
        actual.Add(
            new XElement(Ns + "TablixColumnHierarchy", new XElement(Ns + "TablixMembers", new XElement(Ns + "TablixMember"))),
            new XElement(Ns + "TablixRowHierarchy", new XElement(Ns + "TablixMembers", new XElement(Ns + "TablixMember"))),
            new XElement(Ns + "TablixBody", new XElement(Ns + "TablixColumns", new XElement(Ns + "TablixColumn", new XElement(Ns + "Width", width))), new XElement(Ns + "TablixRows", new XElement(Ns + "TablixRow", new XElement(Ns + "Height", height), new XElement(Ns + "TablixCells", new XElement(Ns + "TablixCell", new XElement(Ns + "CellContents", new XElement(Ns + "Textbox", new XAttribute("Name", name + "Cell")))))))));
        Refresh();
        return _document.Report.Sections.SelectMany(s => s.Body?.ReportItems ?? []).Single(x => x.Name == name);
    }

    /// <summary>Adds a chart shell without interpreting renderer-specific chart styling.</summary>
    public RdlcReportItem AddChart(string name, string dataSetName, string left, string top, string width, string height, string? containerName = null)
    {
        Find(_document.Report.DataSets, dataSetName, "DataSet");
        return AddItem("Chart", name, left, top, width, height, containerName,
            new XElement(Ns + "DataSetName", dataSetName),
            new XElement(Ns + "ChartCategoryHierarchy", new XElement(Ns + "ChartMembers")),
            new XElement(Ns + "ChartSeriesHierarchy", new XElement(Ns + "ChartMembers")),
            new XElement(Ns + "ChartAreas", new XElement(Ns + "ChartArea", new XAttribute("Name", name + "Area"))));
    }

    public RdlcReportItem AddSubreport(string name, string reportName, string left, string top, string width, string height, string? containerName = null)
    {
        if (string.IsNullOrWhiteSpace(reportName)) throw new RdlcAuthoringException("Subreport.ReportName cannot be empty.");
        return AddItem("Subreport", name, left, top, width, height, containerName, new XElement(Ns + "ReportName", reportName));
    }

    public void SetSubreportParameter(string subreportName, string parameterName, string valueExpression)
    {
        ValidateName(parameterName, "SubreportParameter");
        if (string.IsNullOrWhiteSpace(valueExpression)) throw new RdlcAuthoringException("SubreportParameter.Value cannot be empty.");
        var item = FindItem(subreportName);
        if (item.Name.LocalName != "Subreport") throw new RdlcAuthoringException($"ReportItem '{subreportName}' is not a subreport.");
        var parameters = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Parameters") ?? new XElement(Ns + "Parameters");
        if (parameters.Parent is null) item.Add(parameters);
        var parameter = parameters.Elements().FirstOrDefault(x => x.Name.LocalName == "Parameter" && x.Attribute("Name")?.Value == parameterName);
        if (parameter is null) parameters.Add(new XElement(Ns + "Parameter", new XAttribute("Name", parameterName), new XElement(Ns + "Value", valueExpression)));
        else SetChild(parameter, "Value", valueExpression);
    }

    public void SetBookmark(string itemName, string? expression)
    {
        var item = FindItem(itemName); var bookmark = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Bookmark");
        if (expression is null) { bookmark?.Remove(); return; }
        if (string.IsNullOrWhiteSpace(expression)) throw new RdlcAuthoringException("Bookmark cannot be empty.");
        if (bookmark is null) item.Add(new XElement(Ns + "Bookmark", expression)); else bookmark.Value = expression;
    }

    public void SetDrillthrough(string itemName, string reportName, IReadOnlyDictionary<string, string>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(reportName)) throw new RdlcAuthoringException("Drillthrough.ReportName cannot be empty.");
        var item = FindItem(itemName); var action = item.Elements().FirstOrDefault(x => x.Name.LocalName == "Action") ?? new XElement(Ns + "Action");
        if (action.Parent is null) item.Add(action);
        var drillthrough = action.Elements().FirstOrDefault(x => x.Name.LocalName == "Drillthrough") ?? new XElement(Ns + "Drillthrough");
        if (drillthrough.Parent is null) action.Add(drillthrough);
        SetChild(drillthrough, "ReportName", reportName);
        if (parameters is null) return;
        var container = drillthrough.Elements().FirstOrDefault(x => x.Name.LocalName == "Parameters") ?? new XElement(Ns + "Parameters");
        if (container.Parent is null) drillthrough.Add(container);
        foreach (var pair in parameters) { ValidateName(pair.Key, "DrillthroughParameter"); var p = container.Elements().FirstOrDefault(x => x.Name.LocalName == "Parameter" && x.Attribute("Name")?.Value == pair.Key) ?? new XElement(Ns + "Parameter", new XAttribute("Name", pair.Key)); if (p.Parent is null) container.Add(p); SetChild(p, "Value", pair.Value); }
    }

    public void SetInteractiveSort(string itemName, string expression, string? sortTarget = null, string direction = "Ascending")
    {
        if (direction is not ("Ascending" or "Descending")) throw new RdlcAuthoringException("InteractiveSort.Direction: expected Ascending or Descending.");
        if (string.IsNullOrWhiteSpace(expression)) throw new RdlcAuthoringException("InteractiveSort.SortExpression cannot be empty.");
        var item = FindItem(itemName); var sort = item.Elements().FirstOrDefault(x => x.Name.LocalName == "UserSort") ?? new XElement(Ns + "UserSort");
        if (sort.Parent is null) item.Add(sort);
        SetChild(sort, "SortExpression", expression); SetChild(sort, "SortDirection", direction);
        if (sortTarget is not null) SetChild(sort, "SortTarget", sortTarget);
    }

    public void SetToggleItem(string itemName, string? toggleItem)
    {
        var item = FindItem(itemName); var toggle = item.Elements().FirstOrDefault(x => x.Name.LocalName == "ToggleItem");
        if (toggleItem is null) { toggle?.Remove(); return; }
        if (string.IsNullOrWhiteSpace(toggleItem)) throw new RdlcAuthoringException("ToggleItem cannot be empty.");
        if (toggle is null) item.Add(new XElement(Ns + "ToggleItem", toggleItem)); else toggle.Value = toggleItem;
    }

    public RdlcReportItem AddExtensionReportItem(string kind, string name, XElement extensionXml, string left, string top, string width, string height, string? containerName = null)
    {
        if (kind is not ("Map" or "GaugePanel")) throw new RdlcAuthoringException($"ReportItem: extension kind '{kind}' is not a supported authoring extension.");
        ArgumentNullException.ThrowIfNull(extensionXml);
        var item = AddItem(kind, name, left, top, width, height, containerName); FindItem(name).Add(new XElement(extensionXml)); return item;
    }

    public void AddTablixColumn(string tablixName, string width)
    {
        var tablix = FindItem(tablixName); EnsureTablix(tablix);
        var columns = tablix.Descendants().First(x => x.Name.LocalName == "TablixColumns");
        columns.Add(new XElement(Ns + "TablixColumn", new XElement(Ns + "Width", width)));
    }

    public void AddTablixRow(string tablixName, string height)
    {
        var tablix = FindItem(tablixName); EnsureTablix(tablix);
        var rows = tablix.Descendants().First(x => x.Name.LocalName == "TablixRows");
        rows.Add(new XElement(Ns + "TablixRow", new XElement(Ns + "Height", height), new XElement(Ns + "TablixCells")));
    }

    public void AddTablixGroup(string tablixName, string groupName, string expression, bool repeatOnNewPage = false)
    {
        ValidateName(groupName, "TablixGroup");
        var tablix = FindItem(tablixName); EnsureTablix(tablix);
        var members = tablix.Descendants().First(x => x.Name.LocalName == "TablixRowHierarchy").Descendants().First(x => x.Name.LocalName == "TablixMembers");
        members.Add(new XElement(Ns + "TablixMember", new XAttribute("Name", groupName), new XElement(Ns + "Group", new XAttribute("Name", groupName), new XElement(Ns + "GroupExpressions", new XElement(Ns + "GroupExpression", expression))), new XElement(Ns + "RepeatOnNewPage", repeatOnNewPage ? "true" : "false")));
    }

    public void SetTablixRepeatHeader(string tablixName, bool repeat)
    {
        var tablix = FindItem(tablixName); EnsureTablix(tablix);
        var members = tablix.Descendants().First(x => x.Name.LocalName == "TablixRowHierarchy").Descendants().First(x => x.Name.LocalName == "TablixMembers").Elements().FirstOrDefault();
        if (members is null) throw new RdlcAuthoringException("Tablix.RowHierarchy: no row member exists.");
        var repeatElement = members.Elements().FirstOrDefault(x => x.Name.LocalName == "RepeatOnNewPage");
        if (repeatElement is null) members.Add(new XElement(Ns + "RepeatOnNewPage", repeat ? "true" : "false")); else repeatElement.Value = repeat ? "true" : "false";
    }

    private XNamespace Ns => _document.Report.NamespaceUri;
    private XElement TextContent(string value) => new(Ns + "Paragraphs", new XElement(Ns + "Paragraph", new XElement(Ns + "TextRuns", new XElement(Ns + "TextRun", new XElement(Ns + "Value", value)))));
    private RdlcReportItem AddSectionTextBox(string sectionName, string name, string value, string left, string top, string width, string height)
    {
        ValidateName(name, "Textbox"); EnsureUnique(Descendants().Where(x => x.Attribute("Name") is not null).Select(x => x.Attribute("Name")!.Value), name, "Textbox");
        ValidateSize(left, nameof(left)); ValidateSize(top, nameof(top)); ValidateSize(width, nameof(width)); ValidateSize(height, nameof(height));
        var reportSection = _document.MutableRoot.Descendants().FirstOrDefault(x => x.Name.LocalName == "ReportSection") ?? throw new RdlcAuthoringException("ReportSection is required.");
        var section = reportSection.Elements().FirstOrDefault(x => x.Name.LocalName == sectionName);
        if (section is null) { section = new XElement(Ns + sectionName); reportSection.Add(section); }
        var items = section.Elements().FirstOrDefault(x => x.Name.LocalName == "ReportItems");
        if (items is null) { items = new XElement(Ns + "ReportItems"); section.Add(items); }
        var item = new XElement(Ns + "Textbox", new XAttribute("Name", name), new XAttribute("Left", left), new XAttribute("Top", top), new XAttribute("Width", width), new XAttribute("Height", height), TextContent(value));
        items.Add(item); Refresh(); return new RdlcReportItem(item);
    }
    private RdlcReportItem AddItem(string kind, string name, string left, string top, string width, string height, string? containerName, params XElement[] children)
    {
        ValidateName(name, kind); EnsureUnique(Descendants().Where(x => x.Attribute("Name") is not null).Select(x => x.Attribute("Name")!.Value), name, kind);
        ValidateSize(left, nameof(left)); ValidateSize(top, nameof(top)); ValidateSize(width, nameof(width)); ValidateSize(height, nameof(height));
        var element = new XElement(Ns + kind, new XAttribute("Name", name), new XAttribute("Left", left), new XAttribute("Top", top), new XAttribute("Width", width), new XAttribute("Height", height), children);
        if (kind is "Rectangle" or "List") element.AddFirst(new XElement(Ns + "ReportItems"));
        if (containerName is null) GetBodyItems().Add(element);
        else (FindItem(containerName).Elements().FirstOrDefault(x => x.Name.LocalName == "ReportItems") ?? throw new RdlcAuthoringException($"ReportItem '{containerName}' cannot contain child items.")).Add(element);
        Refresh();
        return FindItem(name) is var added ? new RdlcReportItem(added) : throw new InvalidOperationException();
    }
    private XElement GetBodyItems() => _document.Report.Sections.FirstOrDefault()?.Body?.ItemsElement ?? throw new RdlcAuthoringException("ReportSection.Body.ReportItems is required.");
    private static void ValidateSize(string value, string property) { if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^(?:\d+(?:\.\d+)?|\.\d+)(in|cm|mm|pt|px)$")) throw new FormatException($"{property} must be a positive report size with a supported unit."); }
    private static void EnsureTablix(XElement item) { if (item.Name.LocalName != "Tablix") throw new RdlcAuthoringException($"ReportItem '{item.Attribute("Name")?.Value}' is not a tablix."); }
    private XElement EnsureContainer(string name) { var existing = _document.MutableRoot.Elements().FirstOrDefault(x => x.Name.LocalName == name); if (existing is not null) return existing; var created = new XElement(Ns + name); _document.MutableRoot.Add(created); return created; }
    private IEnumerable<XElement> Descendants(string? name = null) => name is null ? _document.MutableRoot.Descendants() : _document.MutableRoot.Descendants().Where(x => x.Name.LocalName == name);
    private void Refresh() => _document.Refresh();
    private void EnsureParameterLayoutCell()
    {
        var layout = _document.MutableRoot.Elements().FirstOrDefault(x => x.Name.LocalName == "ReportParametersLayout");
        var cells = layout?.Descendants().Where(x => x.Name.LocalName == "CellDefinition").ToList();
        if (layout is null || cells is null) return;
        var definition = new XElement(Ns + "CellDefinition", new XElement(Ns + "ColumnIndex", cells.Count.ToString()), new XElement(Ns + "RowIndex", "0"));
        (layout.Descendants().FirstOrDefault(x => x.Name.LocalName == "GridLayoutDefinition") ?? layout).Add(definition);
    }
    private XElement FindItem(string name) => _document.MutableRoot.Descendants().FirstOrDefault(x => x.Attribute("Name")?.Value == name) ?? throw new RdlcAuthoringException($"ReportItem.Name: '{name}' does not exist.");
    private void AddExpressionChild(string itemName, string containerName, string childName, string childNameValue, string expression)
    {
        var item = FindItem(itemName); var container = item.Elements().FirstOrDefault(x => x.Name.LocalName == containerName + "s") ?? new XElement(Ns + containerName + "s");
        if (container.Parent is null) item.Add(container);
        var child = new XElement(Ns + childName, new XAttribute("Name", childNameValue), new XElement(Ns + "GroupExpressions", new XElement(Ns + "GroupExpression", expression)));
        container.Add(child);
    }
    private void SetChild(XElement parent, string name, string value)
    {
        var child = parent.Elements().FirstOrDefault(x => x.Name.LocalName == name);
        if (child is null) parent.Add(new XElement(Ns + name, value)); else child.Value = value;
    }
    private static void ValidateName(string name, string type) { if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_]*$")) throw new RdlcAuthoringException($"{type}.Name: '{name}' is not a valid RDLC identifier."); }
    private static void EnsureUnique(IEnumerable<string?> names, string name, string type) { if (names.Any(x => string.Equals(x, name, StringComparison.Ordinal))) throw new RdlcAuthoringException($"{type}.Name: '{name}' already exists."); }
    private static T Find<T>(IEnumerable<T> items, string name, string type) where T : class { var result = items.FirstOrDefault(x => (x switch { RdlcDataSource s => s.Name, RdlcDataSet d => d.Name, RdlcReportParameter p => p.Name, _ => null }) == name); return result ?? throw new RdlcAuthoringException($"{type}.Name: '{name}' does not exist."); }
    private static void SetValue(XElement parent, string childName, string oldValue, string newValue) { var child = parent.Elements().FirstOrDefault(x => x.Name.LocalName == childName); if (child?.Value == oldValue) child.Value = newValue; }
}

public sealed class RdlcAuthoringException(string message) : InvalidOperationException(message);

public sealed record RdlcDataSetField(string Name, string? DataType = null);

