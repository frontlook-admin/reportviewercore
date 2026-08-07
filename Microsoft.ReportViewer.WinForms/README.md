# This project is not supported/developed by Microsoft

This project is a recompilation of WinForms ReportViewer (part of Microsoft SQL Server Reporting Services) to .NET 8.

Sources and examples are available at https://github.com/lkosson/reportviewercore/

## Viewer appearance

The WinForms viewer uses a lightweight modern light theme by default. Applications can opt into the bundled dark theme or provide their own colors without changing report rendering, export, or page settings:

```csharp
reportViewer.Theme = ReportViewerTheme.Dark;
```

Theme changes only update the toolbar, report canvas, page frame, context menu, and error surface. Page rendering resources are cached and the report data/rendering pipeline is unchanged.

In the report canvas, hold `Ctrl` and scroll the mouse wheel to zoom in or out. Normal wheel scrolling continues to scroll the report.
