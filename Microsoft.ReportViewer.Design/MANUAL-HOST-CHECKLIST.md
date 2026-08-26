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

## R5 verification run (2026-08-26)

Environment: Windows 11 build 10.0.26200, Visual Studio 2022 version 18.9.12112.369 (Enterprise), .NET SDK 10.0.400 with .NET 8.0.424 installed. The host was disposable and located under `C:\Users\deban\AppData\Local\Temp\reportviewer-r5`; no RDLC document was opened or modified.

Verified non-interactively:

- Debug build with isolated output: `dotnet build Microsoft.ReportViewer.Design/Microsoft.ReportViewer.Design.csproj --no-restore --configuration Debug --output C:/Users/deban/AppData/Local/Temp/reportviewer-r5/debug` — succeeded, 0 errors; output contains `Microsoft.ReportViewer.Design.dll`.
- Release build with isolated output: equivalent Release command — succeeded, 0 errors; output contains `Microsoft.ReportViewer.Design.dll`.
- Both design assemblies report identity version `15.0.0.0` and public-key token `A79564977CD39830`.
- Release package: `dotnet pack ... --configuration Release --output C:/Users/deban/AppData/Local/Temp/reportviewer-r5/pack` — succeeded; created `ReportViewerCore.Design.15.0.0.nupkg`. Package contains `lib/net8.0-windows7.0/Microsoft.ReportViewer.Design.dll` and the nuspec.
- Disposable consumer host referencing both `Microsoft.ReportViewer.WinForms` and `Microsoft.ReportViewer.Design` — build succeeded with 0 errors. A runtime smoke executable instantiated `ReportViewer`, set `LocalReport` and `ServerReport` display names, printed the control size/property values, and disposed successfully (`disposed=true`). This proves consumer-host compilation and basic runtime assembly/control/disposal behavior, not live designer behavior.
- `dotnet test Microsoft.ReportViewer.WinForms.Tests/Microsoft.ReportViewer.WinForms.Tests.csproj --no-restore --configuration Release --filter FullyQualifiedName~DesignTimeMetadataTests` — 5 passed, 0 failed, 0 skipped.
- `git diff --check` — passed; pre-existing dirty files remain unchanged apart from this checklist entry.

Live Visual Studio designer attempt: Visual Studio 2022 was launched against the disposable host in an isolated `R5Test` root suffix. A separate `Host - Microsoft Visual Studio` window was surfaced and loaded, but the available desktop interaction approval timed out when attempting the next UI action (`Ctrl+O`). The host therefore did not reach a visible WinForms designer surface or Toolbox insertion/property-grid interaction. Toolbox insertion, designer-load rendering, resize/selection, `LocalReport`/`ServerReport` property editing, CodeDOM serialization, save-close-reopen reload, and live designer disposal remain explicitly unverified. The existing Visual Studio session was not closed or modified.

The runtime host was exercised without an RDLC document and without a real report/data source. Vulnerability and legacy API/compiler warnings remain from existing dependencies/source; they do not change the pass/fail results above.
