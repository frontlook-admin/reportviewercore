using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using System.Drawing.Printing;
using Microsoft.Reporting.WinForms;
using Xunit.Abstractions;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class RdlcPaginationAndPrintRegressionTests
{
    private readonly ITestOutputHelper _output;

    public RdlcPaginationAndPrintRegressionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TwoPageTablix_RecordsEstimatedAndActualCountsSeparately()
    {
        using var report = CreateReport();
        var estimated = report.GetTotalPages(out var estimateMode);

        var renderedStreams = RenderPages(report).Concat(RenderPages(report)).ToList();
        var actual = report.GetTotalPages(out var actualMode);

        _output.WriteLine($"Report.GetTotalPages estimate={estimated} mode={estimateMode}");
        _output.WriteLine($"Report.GetTotalPages actual={actual} mode={actualMode}");
        _output.WriteLine($"Render emfStreams={renderedStreams.Count} bytes={renderedStreams.Sum(stream => stream.Length)}");

        var pdf = report.Render("PDF", string.Empty, PageCountMode.Actual, out _, out _, out _, out _, out _);
        var pdfPageCount = Encoding.ASCII.GetString(pdf).Split("/Type /Page").Length - 1;
        _output.WriteLine($"Render PDF bytes={pdf.Length} pages={pdfPageCount}");

        actual.Should().Be(0);
        actualMode.Should().Be(PageCountMode.Estimate);
        renderedStreams.Should().HaveCount(2);
        renderedStreams.Should().OnlyContain(stream => stream.Length > 0);
        pdfPageCount.Should().Be(2);

        using var viewer = new Microsoft.Reporting.WinForms.ReportViewer();
        viewer.LocalReport.LoadReportDefinition(File.OpenText(FixturePath));
        viewer.LocalReport.DataSources.Add(new ReportDataSource("DataSet1", CreateRows()));
        viewer.SetDisplayMode(DisplayMode.PrintLayout);

        SetViewerDisplayMode(viewer, DisplayMode.PrintLayout);
        var fileManager = GetCurrentFileManager(viewer);
        foreach (var renderedStream in renderedStreams)
        {
            var page = CreatePage(fileManager);
            renderedStream.Position = 0;
            renderedStream.CopyTo(page);
        }

        SetFileManagerStatus(fileManager, "Complete");
        var viewerCount = viewer.GetTotalPages(out var viewerMode);
        var printerSettings = viewer.CreateDefaultPrintSettings();

        _output.WriteLine($"ReportViewer.GetTotalPages={viewerCount} mode={viewerMode}");
        _output.WriteLine($"Persisted printer bounds min={printerSettings.MinimumPage} max={printerSettings.MaximumPage} from={printerSettings.FromPage} to={printerSettings.ToPage}");

        // The PDF renderer and completed print-layout cache are authoritative
        // physical counts for this fixture.
        viewerCount.Should().Be(2);
        viewerMode.Should().Be(PageCountMode.Actual);

    }

    [Fact]
    public void TwoPageTablix_FixtureHasStablePageAndTablixShape()
    {
        var fixturePath = FixturePath;
        var document = XDocument.Load(fixturePath);
        var namespaceName = document.Root!.Name.Namespace;

        document.Root!.Element(namespaceName + "ReportSections")
            .Should().NotBeNull();
        document.Descendants(namespaceName + "Tablix")
            .Select(x => (string?)x.Attribute("Name"))
            .Should().Contain("TwoPageTablix");
        document.Descendants(namespaceName + "PageHeight")
            .Select(x => x.Value)
            .Should().ContainSingle("11in");
        document.Descendants(namespaceName + "PageWidth")
            .Select(x => x.Value)
            .Should().ContainSingle("8.5in");
    }

    [Fact]
    public void TablixRowTargets_AreSortedByTopLeftAndSource()
    {
        var reportType = typeof(Microsoft.Reporting.WinForms.ReportViewer).Assembly
            .GetType("Microsoft.Reporting.WinForms.RenderingReport")!;
        var report = RuntimeHelpers.GetUninitializedObject(reportType);
        var addTarget = reportType.GetMethod("AddTablixRowTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var targetsProperty = reportType.GetProperty("TablixRowTargets", BindingFlags.Instance | BindingFlags.NonPublic)!;

        addTarget.Invoke(report, [new RectangleF(20, 10, 100, 5), false, 1, "B", 1]);
        addTarget.Invoke(report, [new RectangleF(10, 10, 100, 5), true, 0, "A", 0]);
        addTarget.Invoke(report, [new RectangleF(10, 10, 100, 5), false, 2, "C", 2]);

        var targets = ((IEnumerable<object>)targetsProperty.GetValue(report)!).ToList();
        targets.Select(target => (string)target.GetType().GetProperty("Source", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!)
            .Should().Equal("A", "C", "B");
        ((bool)targets[0].GetType().GetProperty("IsHeader", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(targets[0])!)
            .Should().BeTrue();
    }

    [Fact]
    public void GdiPage_RowCursorSelectsBoundariesClampsAndExposesFocusPoint()
    {
        var assembly = typeof(Microsoft.Reporting.WinForms.ReportViewer).Assembly;
        var rendererType = assembly.GetType("Microsoft.Reporting.WinForms.ClientGDIRenderer")!;
        var pageType = assembly.GetType("Microsoft.Reporting.WinForms.GdiPage")!;
        var renderingReportType = assembly.GetType("Microsoft.Reporting.WinForms.RenderingReport")!;
        var renderer = RuntimeHelpers.GetUninitializedObject(rendererType);
        var renderingReport = RuntimeHelpers.GetUninitializedObject(renderingReportType);
        var addTarget = renderingReportType.GetMethod("AddTablixRowTarget", BindingFlags.Instance | BindingFlags.NonPublic)!;
        for (var i = 0; i < 4; i++)
        {
            addTarget.Invoke(renderingReport, [new RectangleF(10, 10 + i * 10, 100, 8), i == 0, i, $"row-{i}", i]);
        }
        rendererType.GetField("m_report", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(renderer, renderingReport);
        var page = Activator.CreateInstance(pageType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [renderer], null)!;
        var move = pageType.GetMethod("MoveSelectedRow", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var selectedIndex = pageType.GetProperty("SelectedRowTargetIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var focusPoint = pageType.GetProperty("SelectedRowFocusPoint", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var rendererReport = rendererType.GetProperty("Report", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var targets = (System.Collections.ICollection)rendererReport.GetValue(renderer)!.GetType()
            .GetProperty("TablixRowTargets", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(rendererReport.GetValue(renderer)!)!;

        targets.Count.Should().BeGreaterThan(1);
        move.Invoke(page, [false]);
        selectedIndex.GetValue(page).Should().Be(0);
        focusPoint.GetValue(page).Should().NotBe(PointF.Empty);

        for (var i = 0; i < targets.Count + 2; i++)
        {
            move.Invoke(page, [false]);
        }
        selectedIndex.GetValue(page).Should().Be(targets.Count - 1);

        for (var i = 0; i < targets.Count + 2; i++)
        {
            move.Invoke(page, [true]);
        }
        selectedIndex.GetValue(page).Should().Be(0);

        var newPage = Activator.CreateInstance(pageType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [renderer], null)!;
        selectedIndex.GetValue(newPage).Should().Be(-1);
    }

    [Fact]
    public void ReportPrintDocument_AllPages_UsesCompletedPagesWhenMaximumPageIsZero()
    {
        using var report = CreateReport();
        var streams = RenderPages(report).Concat(RenderPages(report)).ToList();
        var fileManager = CreateFileManager(streams);
        var settings = new PrinterSettings
        {
            PrintRange = PrintRange.AllPages,
            MinimumPage = 1,
            MaximumPage = 0
        };

        var results = PrintPages(fileManager, settings, streams.Count);

        results.RequestedPages.Should().Equal(1, 2);
        results.HasMorePages.Should().Equal(true, false);
    }

    [Fact]
    public void ReportPrintDocument_SomePages_UsesOneBasedSelectedRange()
    {
        using var report = CreateReport();
        var streams = RenderPages(report).Concat(RenderPages(report)).ToList();
        var fileManager = CreateFileManager(streams);
        var settings = new PrinterSettings
        {
            PrintRange = PrintRange.SomePages,
            FromPage = 1,
            ToPage = 2,
            MinimumPage = 1,
            MaximumPage = 2
        };

        var results = PrintPages(fileManager, settings, streams.Count);

        results.RequestedPages.Should().Equal(1, 2);
        results.HasMorePages.Should().Equal(true, false);
    }

    [Fact]
    public void ReportPrintDocument_SomePages_RejectsZeroBasedRange()
    {
        using var report = CreateReport();
        var streams = RenderPages(report).Concat(RenderPages(report)).ToList();
        var fileManager = CreateFileManager(streams);
        var settings = new PrinterSettings
        {
            PrintRange = PrintRange.SomePages,
            FromPage = 0,
            ToPage = 2
        };
        var document = CreatePrintDocument(fileManager, settings);

        var beginPrint = typeof(PrintDocument).GetMethod("OnBeginPrint", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var args = new PrintEventArgs();

        Action act = () => beginPrint.Invoke(document, [args]);

        act.Should().Throw<TargetInvocationException>()
            .Which.InnerException.Should().BeOfType<ArgumentOutOfRangeException>();
    }

    private static LocalReport CreateReport()
    {
        var report = new LocalReport();
        using var definition = File.OpenText(FixturePath);
        report.LoadReportDefinition(definition);
        report.DataSources.Add(new ReportDataSource("DataSet1", CreateRows()));
        return report;
    }

    private static DataTable CreateRows()
    {
        var rows = new DataTable();
        rows.Columns.Add("Id", typeof(int));
        rows.Columns.Add("Label", typeof(string));
        for (var id = 1; id <= 0; id++)
        {
            rows.Rows.Add(id, $"Row {id:00}");
        }

        return rows;
    }

    private static List<MemoryStream> RenderPages(LocalReport report)
    {
        var streams = new List<MemoryStream>();
        report.Render(
            "IMAGE",
            "<DeviceInfo><OutputFormat>emf</OutputFormat><StartPage>1</StartPage><EndPage>0</EndPage></DeviceInfo>",
            PageCountMode.Actual,
            (name, extension, encoding, mimeType, willSeek) =>
            {
                var stream = new MemoryStream();
                streams.Add(stream);
                return stream;
            },
            out _);
        return streams;
    }

    private static object CreateFileManager(IEnumerable<MemoryStream> streams)
    {
        var fileManagerType = typeof(Microsoft.Reporting.WinForms.ReportViewer).Assembly
            .GetType("Microsoft.Reporting.WinForms.FileManager")!;
        var fileManager = Activator.CreateInstance(fileManagerType, nonPublic: true)!;
        var createPage = fileManagerType.GetMethod("CreatePage")!;
        foreach (var source in streams)
        {
            var page = (Stream)createPage.Invoke(fileManager, [true])!;
            source.Position = 0;
            source.CopyTo(page);
        }
        fileManagerType.GetProperty("Status")!.SetValue(fileManager, ParseFileManagerStatus("Complete"));
        return fileManager;
    }

    private static object CreatePrintDocument(object fileManager, PrinterSettings settings)
    {
        var documentType = typeof(Microsoft.Reporting.WinForms.ReportViewer).Assembly
            .GetType("Microsoft.Reporting.WinForms.ReportPrintDocument")!;
        var pageSettings = new PageSettings { PrinterSettings = settings };
        var document = (PrintDocument)Activator.CreateInstance(documentType, BindingFlags.Instance | BindingFlags.NonPublic, null, [fileManager, pageSettings], null)!;
        document.PrinterSettings = settings;
        return document;
    }

    private static (List<int> RequestedPages, List<bool> HasMorePages) PrintPages(object fileManager, PrinterSettings settings, int pageCount)
    {
        var document = CreatePrintDocument(fileManager, settings);
        var beginPrint = typeof(PrintDocument).GetMethod("OnBeginPrint", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var printPage = typeof(PrintDocument).GetMethod("OnPrintPage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        beginPrint.Invoke(document, [new PrintEventArgs()]);
        var requested = new List<int>();
        var more = new List<bool>();
        using var bitmap = new Bitmap(850, 1100);
        using var graphics = Graphics.FromImage(bitmap);
        for (var i = 0; i < pageCount; i++)
        {
            requested.Add(i + 1);
            var pageArgs = new PrintPageEventArgs(graphics, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(0, 0, bitmap.Width, bitmap.Height), settings.DefaultPageSettings);
            printPage.Invoke(document, [pageArgs]);
            more.Add(pageArgs.HasMorePages);
            if (!pageArgs.HasMorePages)
            {
                break;
            }
        }
        return (requested, more);
    }

    private static string FixturePath => Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "Fixtures",
        "TwoPageTablix.rdlc");

    private static object GetCurrentFileManager(Microsoft.Reporting.WinForms.ReportViewer viewer)
    {
        var currentReport = typeof(Microsoft.Reporting.WinForms.ReportViewer)
            .GetProperty("CurrentReport", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(viewer)!;
        return currentReport.GetType().GetProperty("FileManager")!.GetValue(currentReport)!;
    }

    private static object ParseFileManagerStatus(string name)
    {
        var statusType = typeof(Microsoft.Reporting.WinForms.ReportViewer).Assembly.GetType("Microsoft.Reporting.WinForms.FileManagerStatus")!;
        return Enum.Parse(statusType, name);
    }

    private static Stream CreatePage(object fileManager)
    {
        return (Stream)fileManager.GetType().GetMethod("CreatePage")!.Invoke(fileManager, [true])!;
    }

    private static void SetFileManagerStatus(object fileManager, string value)
    {
        fileManager.GetType().GetProperty("Status")!.SetValue(fileManager, ParseFileManagerStatus(value));
    }

    private static string GetFileManagerStatus(object fileManager)
    {
        return fileManager.GetType().GetProperty("Status")!.GetValue(fileManager)!.ToString()!;
    }

    private static int GetFileManagerCount(object fileManager)
    {
        return (int)fileManager.GetType().GetProperty("Count")!.GetValue(fileManager)!;
    }

    private static void SetViewerDisplayMode(Microsoft.Reporting.WinForms.ReportViewer viewer, DisplayMode mode)
    {
        typeof(Microsoft.Reporting.WinForms.ReportViewer)
            .GetField("m_viewMode", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(viewer, mode);
    }
}
