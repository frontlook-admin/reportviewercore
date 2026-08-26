using Microsoft.Reporting.WinForms;
using System.ComponentModel;
using Xunit;

namespace Microsoft.ReportViewer.Design.Tests;

public sealed class RdlcDesignEditingTests
{
    [Fact]
    public void Transaction_commit_marks_document_dirty_and_undo_redo_restores_value()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Page><PageWidth>8.5in</PageWidth></Page></ReportSection></ReportSections>"));
        var session = new RdlcDesignerSession(document);
        using (var transaction = session.BeginTransaction("Change page width"))
        {
            session.SetProperty(document.Report.Sections[0].Page!, "PageWidth", "9in");
            transaction.Commit();
        }

        Assert.True(session.IsDirty);
        Assert.Equal("9in", document.Report.Sections[0].Page!.PageWidth);
        Assert.True(session.Undo());
        Assert.Equal("8.5in", document.Report.Sections[0].Page!.PageWidth);
        Assert.True(session.Redo());
        Assert.Equal("9in", document.Report.Sections[0].Page!.PageWidth);
    }

    [Fact]
    public void Invalid_typed_value_is_rejected_without_dirtying_document()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Page><PageWidth>8.5in</PageWidth></Page></ReportSection></ReportSections>"));
        var session = new RdlcDesignerSession(document);

        Assert.Throws<FormatException>(() => session.SetProperty(document.Report.Sections[0].Page!, "PageWidth", "wide"));
        Assert.False(session.IsDirty);
        Assert.Equal("8.5in", document.Report.Sections[0].Page!.PageWidth);
    }

    [Fact]
    public void Cancel_rolls_back_and_runtime_only_item_properties_are_not_exposed()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Body><ReportItems><Textbox Name=\"Title\" Left=\"1in\" /></ReportItems></Body></ReportSection></ReportSections>"));
        var session = new RdlcDesignerSession(document);
        using (session.BeginTransaction("Cancelled"))
        {
            session.SetProperty(document.Report.Sections[0].Body!.ReportItems[0], "Name", "Changed");
        }

        Assert.Equal("Title", document.Report.Sections[0].Body!.ReportItems[0].Name);
        Assert.False(session.IsDirty);
        var names = session.GetProperties(document.Report.Sections[0].Body!.ReportItems[0]).Cast<PropertyDescriptor>().Select(x => x.Name).ToArray();
        Assert.Contains("Name", names);
        Assert.DoesNotContain("CurrentPage", names);
        Assert.DoesNotContain("LocalReport", names);
    }

    [Fact]
    public void Copy_paste_preserves_unknown_item_xml_and_undo_order_is_newest_first()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Body><ReportItems><Textbox Name=\"Title\"><x:Keep xmlns:x=\"urn:x\" a=\"b\" /></Textbox></ReportItems></Body></ReportSection></ReportSections>"));
        var session = new RdlcDesignerSession(document);
        var body = document.Report.Sections[0].Body!;
        var fragment = session.Copy(body.ReportItems[0]);
        session.Paste(body, fragment);
        Assert.Equal(2, body.ReportItems.Count);
        Assert.Contains("x:Keep", document.ToXml());
        Assert.True(session.Undo());
        Assert.Single(body.ReportItems);
        Assert.True(session.Redo());
        Assert.Equal(2, body.ReportItems.Count);
    }

    [Fact]
    public void Core_report_items_can_be_created_nested_and_round_trip()
    {
        var document = RdlcDocument.Load(Xml("<ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);

        model.AddTextBox("Title", "Hello", "1in", "1in", "3in", ".5in");
        model.AddRectangle("Panel", "0in", "0in", "6in", "3in");
        model.AddImage("Logo", "LogoImage", "4in", "1in", "1in", "1in");
        model.AddLine("Rule", "1in", "2in", "3in", ".02in");
        model.AddTextBox("Nested", "Inside", "0in", "0in", "1in", ".25in", "Panel");
        model.AddTextRun("Title", " world", "Bold");

        Assert.Equal(new[] { "Title", "Panel", "Logo", "Rule" }, document.Report.Sections[0].Body!.ReportItems.Select(x => x.Name));
        Assert.Contains("<ReportItems><Textbox Name=\"Nested\"", document.ToXml());
        Assert.Contains("<FontWeight>Bold</FontWeight>", document.ToXml());
        Assert.Equal(document.ToXml(), RdlcDocument.Load(document.ToXml()).ToXml());
    }

    [Fact]
    public void Tablix_rows_columns_groups_and_formatting_are_editable()
    {
        var document = RdlcDocument.Load(Xml("<DataSets><DataSet Name=\"Orders\" /></DataSets><ReportSections><ReportSection><Body><ReportItems /></Body></ReportSection></ReportSections>"));
        var model = new RdlcAuthoringModel(document);

        model.AddTablix("Grid", "Orders", "0in", "0in", "5in", "2in");
        model.AddTablixColumn("Grid", "1in");
        model.AddTablixRow("Grid", ".3in");
        model.AddTablixGroup("Grid", "OrdersGroup", "=Fields!OrderId.Value");
        model.SetItemFormatting("Grid", "BackgroundColor", "#ffffff");
        model.SetItemVisibility("Grid", "=Fields!Hidden.Value");

        var xml = document.ToXml();
        Assert.Contains("<TablixColumnHierarchy>", xml);
        Assert.Contains("<TablixRowHierarchy>", xml);
        Assert.Contains("<TablixMember Name=\"OrdersGroup\"", xml);
        Assert.Contains("<BackgroundColor>#ffffff</BackgroundColor>", xml);
        Assert.Contains("<Hidden>=Fields!Hidden.Value</Hidden>", xml);
        Assert.Equal(xml, RdlcDocument.Load(xml).ToXml());
    }

    private static string Xml(string content) => $"<Report xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition\">{content}</Report>";
}
