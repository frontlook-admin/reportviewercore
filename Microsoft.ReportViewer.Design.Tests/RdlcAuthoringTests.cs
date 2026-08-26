using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.Design.Tests;

public sealed class RdlcAuthoringTests
{
    [Fact]
    public void Authoring_renames_references_and_preserves_extensions()
    {
        var document = RdlcDocument.Load(Xml("<x:Keep xmlns:x=\"urn:keep\"/><DataSources><DataSource Name=\"Orders\"/></DataSources><DataSets><DataSet Name=\"OrdersSet\"><Query><DataSourceName>Orders</DataSourceName></Query></DataSet></DataSets><ReportSections><ReportSection><Body><ReportItems><Tablix Name=\"Grid\"><DataSetName>OrdersSet</DataSetName></Tablix></ReportItems></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);

        model.RenameDataSource("Orders", "Sales");
        model.RenameDataSet("OrdersSet", "SalesSet");

        Assert.Equal("Sales", document.Report.DataSets[0].DataSourceName);
        Assert.Contains("<DataSetName>SalesSet</DataSetName>", document.ToXml());
        Assert.Contains("x:Keep", document.ToXml());
        Assert.Empty(model.ValidateReferences());
    }

    [Fact]
    public void Parameter_layout_and_expression_diagnostics_are_actionable()
    {
        var document = RdlcDocument.Load(Xml("<ReportParametersLayout><GridLayoutDefinition><CellDefinitions><CellDefinition><ColumnIndex>0</ColumnIndex><RowIndex>0</RowIndex></CellDefinition></CellDefinitions></GridLayoutDefinition></ReportParametersLayout><ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);
        model.AddParameter("Period", "Integer", "2", hidden: true);
        var diagnostics = model.ValidateExpression("Grid.Value", "Value", "=Parameters!Missing!Value");

        Assert.Contains("<Hidden>true</Hidden>", document.ToXml());
        Assert.Equal(2, document.RootXml.Descendants().Count(x => x.Name.LocalName == "CellDefinition"));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(("Grid.Value", "Value"), (diagnostic.Item, diagnostic.Property));
        Assert.Contains("Missing", diagnostic.Message);
    }

    [Fact]
    public void Delete_rejects_referenced_items_and_completion_lists_fields_and_parameters()
    {
        var document = RdlcDocument.Load(Xml("<ReportParameters><ReportParameter Name=\"Period\"><DataType>Integer</DataType></ReportParameter></ReportParameters><DataSources><DataSource Name=\"Orders\"/></DataSources><DataSets><DataSet Name=\"OrdersSet\"><Fields><Field Name=\"Amount\"/></Fields><Query><DataSourceName>Orders</DataSourceName></Query></DataSet></DataSets><ReportSections><ReportSection><Body><ReportItems><Textbox Name=\"AmountText\"><Value>=Fields!Amount.Value + Parameters!Period.Value</Value></Textbox></ReportItems></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);

        var error = Assert.Throws<RdlcAuthoringException>(() => model.DeleteParameter("Period"));
        Assert.Contains("expression", error.Message);
        Assert.Equal(new[] { "Amount" }, model.CompleteExpression("=Fields!", 8));
        Assert.Equal(new[] { "Period" }, model.CompleteExpression("=Parameters!", 12));
    }

    private static string Xml(string content) => $"<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition\">{content}</Report>";
}
