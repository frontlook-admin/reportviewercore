using CliReportCompiler;
using FrontLookCoreDbAccessLibrary.Desktop.Rdlc.FL_RDLC;
using Microsoft.Reporting.WinForms;
using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace RdlcCliReportCompiler.Tests;

public sealed class ReportCompilerUtilityTests
{
    [Fact]
    public void ParseArguments_NormalizesLongShortAndQuotedValues()
    {
        var parameters = Parse(
            "--REPORTPATH", "\"C:\\Reports Folder\\report.rdlc\"",
            "-DS", "data.xml",
            "--ReportName", "Report",
            "-M", "PrintSettings",
            "--PrintSetupFile", "settings.json");

        Assert.Equal("C:\\Reports Folder\\report.rdlc", parameters["ReportPath"]);
        Assert.Equal("data.xml", parameters["ReportDataSource"]);
        Assert.Equal("PrintSettings", parameters["Mode"]);
    }

    [Fact]
    public void SplitCommandLineArgs_PreservesQuotedSpaces()
    {
        var args = ReportCompilerUtility.SplitCommandLineArgs(
            "--ReportPath \"C:\\Reports Folder\\report.rdlc\" --Mode Preview");

        Assert.Equal(new[] { "--ReportPath", "C:\\Reports Folder\\report.rdlc", "--Mode", "Preview" }, args);
    }

    [Fact]
    public void ParseArguments_RejectsUnknownOptionsAndClearsPreviousState()
    {
        Parse("--Mode", "Preview");

        var exception = Assert.Throws<ArgumentException>(() =>
            ReportCompilerUtility.ParseArguments(new[] { "--Unknown", "value" }));

        Assert.Contains("Unknown option", exception.Message);
        Assert.Empty(ReportCompilerUtility.GetCurrentParameters());
    }

    [Fact]
    public void ParseArguments_ClearsSubReportsBetweenExecutions()
    {
        var subReportPath = Path.GetTempFileName();
        try
        {
            Parse(
                "--AttachSubReport", $"Detail={subReportPath}",
                "--Mode", "Preview");
            Assert.True(ReportCompilerUtility.GetCurrentParameters().ContainsKey("AttachSubReport"));

            Parse("--Mode", "Preview");

            Assert.False(ReportCompilerUtility.GetCurrentParameters().ContainsKey("AttachSubReport"));
        }
        finally
        {
            File.Delete(subReportPath);
        }
    }

    [Fact]
    public void ParseArguments_SupportsViewerWaitAndDetailedErrorLoggingOptions()
    {
        var parameters = Parse(
            "--WaitForViewer", "false",
            "--EnableErrorLogging", "true",
            "--Mode", "Preview");

        Assert.Equal("false", parameters["WaitForViewer"]);
        Assert.Equal("true", parameters["EnableErrorLogging"]);
    }

    [Fact]
    public void ParseArguments_SupportsVerboseFileLoggingAndContinuationOptions()
    {
        var parameters = Parse(
            "--Verbose", "true",
            "--LogFile", "logs\\compiler.jsonl",
            "--ContinueOnError", "false",
            "--Mode", "Preview");

        Assert.Equal("true", parameters["Verbose"]);
        Assert.Equal("logs\\compiler.jsonl", parameters["LogFile"]);
        Assert.Equal("false", parameters["ContinueOnError"]);
    }

    [Fact]
    public void ParseArguments_SupportsBatchValidationAndOpenExportOptions()
    {
        var parameters = Parse(
            "--BatchFile", "commands.txt",
            "--Validate",
            "--OpenExport");

        Assert.Equal("commands.txt", parameters["BatchFile"]);
        Assert.Equal("true", parameters["Validate"]);
        Assert.Equal("true", parameters["OpenExport"]);
    }

    [Fact]
    public void ParseArguments_AllowsBooleanOptionsWithoutValues()
    {
        var parameters = Parse(
            "--WaitForViewer",
            "--EnableErrorLogging",
            "--Mode", "Preview");

        Assert.Equal("true", parameters["WaitForViewer"]);
        Assert.Equal("true", parameters["EnableErrorLogging"]);
    }

    [Fact]
    public void ParseArguments_RejectsInvalidBooleanOptions()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ReportCompilerUtility.ParseArguments(new[] { "--WaitForViewer", "sometimes" }));

        Assert.Contains("WaitForViewer must be true or false", exception.Message);
    }

    [Theory]
    [InlineData("PDF", ExportFormat.PDF)]
    [InlineData("excel", ExportFormat.EXCEL)]
    [InlineData("EXCELOPENXML", ExportFormat.EXCELOPENXML)]
    [InlineData("HTML5", ExportFormat.HTML5)]
    public void GetExportFormat_MapsSupportedFormats(string format, ExportFormat expected)
    {
        Parse("--ExportFormat", format);

        Assert.Equal(expected, ReportCompilerUtility.GetExportFormat());
    }

    [Fact]
    public void GetExportFormat_RejectsUnsupportedFormats()
    {
        Parse("--ExportFormat", "CSV");

        var exception = Assert.Throws<ArgumentException>(() => ReportCompilerUtility.GetExportFormat());

        Assert.Contains("Unsupported export format", exception.Message);
    }

    [Theory]
    [InlineData(typeof(ArgumentException), 2)]
    [InlineData(typeof(FileNotFoundException), 3)]
    [InlineData(typeof(InvalidDataException), 4)]
    [InlineData(typeof(OperationCanceledException), 5)]
    [InlineData(typeof(InvalidOperationException), 1)]
    public void GetExitCode_MapsFailureCategories(Type exceptionType, int expectedCode)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;

        Assert.Equal(expectedCode, ReportCompilerUtility.GetExitCode(exception));
    }

    [Fact]
    public void GetDefaultPrintSettingFilePath_UsesApplicationDirectoryAndReportName()
    {
        var path = ReportViewer.GetDefaultPrintSettingFilePath("SalesReport.rdlc");

        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "RdlcPrintSetting", "SalesReport.json"),
            path);
    }

    [Fact]
    public void LogError_WritesStructuredJsonLogFile()
    {
        var logFile = Path.Combine(Path.GetTempPath(), $"rdlc-cli-{Guid.NewGuid():N}.jsonl");
        try
        {
            Parse(
                "--LogFile", logFile,
                "--EnableErrorLogging", "true",
                "--Mode", "Preview");

            ReportCompilerUtility.LogError(new InvalidOperationException("test failure"));

            var line = File.ReadAllLines(logFile).Single();
            using var document = JsonDocument.Parse(line);
            Assert.Equal("Error", document.RootElement.GetProperty("level").GetString());
            Assert.Equal("error", document.RootElement.GetProperty("event").GetString());
            Assert.Contains("test failure", document.RootElement.GetProperty("exception").GetString());
        }
        finally
        {
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        }
    }

    [Fact]
    public void SplitCommandLineArgs_RejectsUnterminatedQuotes()
    {
        Assert.Throws<ArgumentException>(() =>
            ReportCompilerUtility.SplitCommandLineArgs("--Mode \"Preview"));
    }

    private static Dictionary<string, string> Parse(params string[] args)
    {
        Assert.True(ReportCompilerUtility.ParseArguments(args));
        return ReportCompilerUtility.GetCurrentParameters();
    }
}
