# This project is not supported/developed by Microsoft

This project is a recompilation of WinForms ReportViewer (part of Microsoft SQL Server Reporting Services) to .NET 8.

Sources and examples are available at https://github.com/lkosson/reportviewercore/

## Viewer appearance

The WinForms viewer uses a lightweight modern light theme by default. Applications can opt into the bundled dark theme or provide their own colors without changing report rendering, export, or page settings:

```csharp
reportViewer.Theme = ReportViewerTheme.Dark;
```

Theme changes update the toolbar, report canvas, page frame, context menu, error surface, and on-screen report colors. The mapping preserves report colors that already contrast with the selected background. Printing and export retain the original report colors; page rendering resources and the report data/rendering pipeline are unchanged.

In the report canvas, hold `Ctrl` and scroll the mouse wheel to zoom in or out. Normal wheel scrolling continues to scroll the report.

The toolbar also provides a Theme menu for Light, Dark, and High contrast modes. The theme can be changed at runtime without reloading the report. The status bar can be hidden when a compact layout is preferred:

```csharp
reportViewer.ShowStatusBar = false;
```

Viewer keyboard shortcuts:

- `Ctrl` + `+` / `-`: zoom in or out.
- `Ctrl` + `0`: reset zoom to 100%.
- `Ctrl` + `F`: focus report search.
- `PageUp` / `PageDown`: previous or next page.
- `Home` / `End`: first or last page.
- `F5`: refresh the report.
- `Esc`: cancel an active rendering operation.

Search terms entered in the toolbar are kept in a bounded in-memory history for the current viewer instance. Export dialogs remember the last export folder and can open the completed file automatically.
