# ReportViewer WinForms designer host checklist

This checklist is for a disposable Visual Studio WinForms consumer. It is
intentionally separate from automated build and reflection tests: those tests
prove the assembly contract, not Visual Studio's live designer process.

## Setup

1. Build `Microsoft.ReportViewer.WinForms` and
   `Microsoft.ReportViewer.Design` for `net8.0-windows`/`net8.0-windows7`.
2. Create a temporary SDK-style `net8.0-windows` WinForms application.
3. Reference the runtime and design assemblies (or install the matching
   `ReportViewerCore.WinForms` and `ReportViewerCore.Design` packages).
4. Open the Toolbox, choose **Choose Items...**, and confirm `ReportViewer`
   appears as a Windows Forms control.

## Host checks

- Drag `ReportViewer` onto a form and confirm the control creates without a
  designer load exception.
- Confirm the Properties window shows normal WinForms properties and the
  report model under the `LocalReport` content property.
- Change a visible appearance property, save, close, and reopen the form.
- Confirm generated `InitializeComponent` code reloads and preserves the
  control and its serialized values.
- Open the ReportViewer smart tag and invoke **Reset viewer appearance**.
- Open the control context menu and invoke the equivalent designer verb.
- Build and run the disposable host, confirming the runtime assembly is used
  without requiring the design assembly at application startup.

## Evidence to record

Record the Visual Studio version, target framework, package/assembly versions,
whether the Toolbox, Properties window, smart tag, verb, CodeDOM reload, and
runtime launch checks passed, plus any designer error-list or activity-log
details. Do not record credentials or machine-specific secrets.