# Microsoft.ReportViewer.Design manual host checklist

Use a Windows machine with Visual Studio 2022 and the supported .NET 8 Windows Desktop SDK.

## Build and load

- [ ] Build `Microsoft.ReportViewer.Design.csproj` in Debug and Release.
- [ ] Confirm the output is named `Microsoft.ReportViewer.Design.dll`.
- [ ] Confirm the assembly identity is version `15.0.0.0` and record the repository signing key token alongside the host result.
- [ ] In a disposable .NET 8 WinForms project, reference both `Microsoft.ReportViewer.WinForms` and `Microsoft.ReportViewer.Design`.
- [ ] Open the WinForms designer and confirm the ReportViewer control can be added from the Toolbox.
- [ ] Confirm the designer loads without a missing-type or designer-load error.

## Control and property surface

- [ ] Drop a ReportViewer onto a form and resize/reposition it.
- [ ] Confirm the control remains selectable and paints its normal design-time surface.
- [ ] Confirm `LocalReport` and `ServerReport` remain expandable/content-serializable properties.
- [ ] Confirm runtime-only state is not emitted into generated `InitializeComponent` code.
- [ ] Save, close, and reopen the form; confirm the control and serialized report properties remain intact.
- [ ] Build and run the disposable host; confirm the runtime assembly is used and the design assembly is not required for application startup.

## Compatibility boundary

- [ ] Do not open or modify an RDLC document as part of this check; RDLC editor support is out of scope for C1.
- [ ] Record the Visual Studio version, SDK version, target framework, and exact design assembly file used with the result.

## C2 verification run (2026-08-26)

The available non-interactive checks passed:

- `dotnet build Microsoft.ReportViewer.Design/Microsoft.ReportViewer.Design.csproj --no-restore --configuration Debug --verbosity minimal` — succeeded, 0 errors.
- `dotnet build Microsoft.ReportViewer.Design/Microsoft.ReportViewer.Design.csproj --no-restore --configuration Release --verbosity minimal` — succeeded, 0 errors.
- `dotnet test Microsoft.ReportViewer.WinForms.Tests/Microsoft.ReportViewer.WinForms.Tests.csproj --no-restore --filter FullyQualifiedName~DesignTimeMetadataTests --verbosity minimal` — 5 passed, 0 failed, 0 skipped.
- `dotnet pack Microsoft.ReportViewer.Design/Microsoft.ReportViewer.Design.csproj --no-restore --configuration Release --output C:\Users\deban\AppData\Local\Temp\reportviewer-c2-pack --verbosity minimal` — created `ReportViewerCore.Design.15.0.0.nupkg`.
- `git diff --check` — passed.

The live Visual Studio designer lane remains unverified in this run. Visual Studio 2022/18.9.12112.369 is installed, but the disposable host could not be surfaced as an interactive designer window through the available desktop session; no Toolbox, Property Grid, serialization/reload, or live designer-disposal result is claimed. The disposable host project did compile against both runtime and design assemblies (`net8.0-windows7`).

The host/runtime checks above do not include opening or modifying an RDLC document. Existing NU1902/NU1903 dependency vulnerability warnings remain; they are unrelated to the design assembly load checks.
