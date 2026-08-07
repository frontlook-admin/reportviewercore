using System;
using System.IO;
using System.Text.Json;

namespace Microsoft.Reporting.WinForms
{
    public enum ReportViewerThemeKind
    {
        Light,
        Dark,
        HighContrast
    }

    /// <summary>
    /// Serializable viewer preferences. Report data, parameters, and print settings
    /// are intentionally kept outside this model.
    /// </summary>
    public sealed class ReportViewerPreferences
    {
        public ReportViewerThemeKind Theme { get; set; } = ReportViewerThemeKind.Light;

        public ZoomMode ZoomMode { get; set; } = ZoomMode.Percent;

        public int ZoomPercent { get; set; } = 100;

        public bool ShowToolBar { get; set; } = true;

        public bool ShowStatusBar { get; set; } = true;

        public bool ShowProgress { get; set; } = true;

        public bool ShowContextMenu { get; set; } = true;

        public bool ShowParameterPrompts { get; set; } = true;

        public bool ShowCredentialPrompts { get; set; } = true;

        public bool PromptAreaCollapsed { get; set; }

        public bool DocumentMapCollapsed { get; set; }

        public int DocumentMapWidth { get; set; } = 100;

        public bool IsDocumentMapWidthFixed { get; set; }

        public bool ShowDocumentMapButton { get; set; } = true;

        public bool ShowPromptAreaButton { get; set; } = true;

        public bool ShowPageNavigationControls { get; set; } = true;

        public bool ShowBackButton { get; set; } = true;

        public bool ShowStopButton { get; set; } = true;

        public bool ShowRefreshButton { get; set; } = true;

        public bool ShowPrintButton { get; set; } = true;

        public bool ShowExportButton { get; set; } = true;

        public bool ShowZoomControl { get; set; } = true;

        public bool ShowFindControls { get; set; } = true;

        internal static ReportViewerPreferences Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A preferences file path is required.", nameof(filePath));
            }

            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        internal static ReportViewerPreferences FromJson(string json)
        {
            return JsonSerializer.Deserialize<ReportViewerPreferences>(json)
                ?? throw new InvalidDataException("The viewer preferences file is empty or invalid.");
        }

        internal string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

    }
}
