# Running the MAUI ReportViewer Sample Application

This guide explains how to build, package, and run the ReportViewer MAUI sample application on Windows.

## Prerequisites

- .NET 8 SDK or later
- Visual Studio 2022 17.8+ with MAUI workload, OR
- .NET MAUI workloads installed via `dotnet workload install maui`
- Windows 10 version 1809 (build 17763) or later
- Windows App SDK 1.5 or later (automatically installed with app)

## Build Methods

### Method 1: Build as MSIX Package (Recommended)

The recommended way to run the MAUI sample is as a packaged MSIX application, which properly bundles all Windows App Runtime dependencies.

#### Step 1: Build the MSIX Package

```powershell
cd G:\Repos\frontlook-admin\reportviewercore
dotnet build "ReportViewerCore.Sample.MAUI\ReportViewerCore.Sample.MAUI.csproj" -c Debug -p:Platform=x64
```

This will:
- Build the application for x64 architecture
- Create a signed MSIX package
- Generate installation files in `ReportViewerCore.Sample.MAUI\bin\x64\Debug\net8.0-windows10.0.19041.0\AppPackages\`

#### Step 2: Install the Application

Navigate to the AppPackages folder and run the install script:

```powershell
cd "ReportViewerCore.Sample.MAUI\bin\x64\Debug\net8.0-windows10.0.19041.0\AppPackages\ReportViewerCore.Sample.MAUI_1.0.0.0_Debug_Test"
.\Install.ps1
```

Follow the prompts:
1. Press Enter to install the signing certificate (requires administrator)
2. The certificate will be installed to your Trusted Root and Trusted People stores
3. The app and its dependencies will be installed
4. Press Enter to complete

#### Step 3: Run the Application

After installation, launch the app from the Start Menu:
- Search for "ReportViewer MAUI Sample"
- Or run via PowerShell:

```powershell
$pkg = Get-AppxPackage | Where-Object {$_.Name -eq 'com.reportviewercore.sample'}
if ($pkg) { 
    explorer.exe "shell:AppsFolder\$($pkg.PackageFamilyName)!App" 
}
```

### Method 2: Development/Debug Run (May Require Additional Setup)

For quick testing during development (note: may encounter Windows App Runtime issues):

```powershell
dotnet run --project "ReportViewerCore.Sample.MAUI\ReportViewerCore.Sample.MAUI.csproj" --framework net8.0-windows10.0.19041.0
```

**Note:** Unpackaged execution may fail with Windows App Runtime errors. If this occurs, use Method 1 (MSIX packaging) instead.

## Project Configuration

The project is configured for MSIX packaging in `ReportViewerCore.Sample.MAUI.csproj`:

```xml
<PropertyGroup>
    <!-- MSIX Packaging for Windows -->
    <WindowsPackageType>MSIX</WindowsPackageType>
    <GenerateAppxPackageOnBuild>true</GenerateAppxPackageOnBuild>
    <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
    <PackageCertificateThumbprint>8F66BA61E2AA3353311409ECE8388B01DAD5F877</PackageCertificateThumbprint>
    <Platforms>x64</Platforms>
</PropertyGroup>
```

## Certificate Management

### Development Certificate

A self-signed certificate is automatically created and included in the MSIX package for development purposes. The certificate is valid only for development and testing.

### For Production

For production deployment:
1. Obtain a code signing certificate from a trusted Certificate Authority
2. Update the `PackageCertificateThumbprint` in the project file
3. Ensure the certificate is in your Personal certificate store
4. Build and distribute the signed package

## Troubleshooting

### Issue: "WindowsPackageType is set to None" error

**Solution:** Ensure `<WindowsPackageType>MSIX</WindowsPackageType>` is set in the project file and `<GenerateAppxPackageOnBuild>` is `true`.

### Issue: "Packaged .NET applications must specify RuntimeIdentifier"

**Solution:** Build with explicit platform: `-p:Platform=x64` (or x86, arm64)

### Issue: "Package deployment failed - not in unsigned namespace"

**Solution:** Run the `Install.ps1` script which installs the certificate first, or manually trust the certificate.

### Issue: App shows COM registration error (0x80040154)

**Solution:** Use packaged MSIX deployment instead of unpackaged. The MSIX includes all required Windows App Runtime dependencies.

### Issue: Windows App Runtime version mismatch

**Solution:** The MSIX package includes the correct Windows App Runtime version (1.5) as a dependency. This is automatically installed during app installation.

## Uninstalling

To remove the application:

```powershell
Get-AppxPackage | Where-Object {$_.Name -eq 'com.reportviewercore.sample'} | Remove-AppxPackage
```

Or uninstall via Settings > Apps > Installed apps

## Platform Support

Currently, the sample application is configured for **Windows only** (net8.0-windows10.0.19041.0).

To enable other platforms, modify the `<TargetFrameworks>` in the project file:

```xml
<TargetFrameworks>net8.0-android;net8.0-ios;net8.0-maccatalyst;net8.0-windows10.0.19041.0</TargetFrameworks>
```

Note: Cross-platform support requires the MAUI workloads for each target platform.

## Architecture

### Components

1. **Microsoft.ReportViewer.MAUI** - Core MAUI control library
2. **ReportViewerCore.Sample.MAUI** - Sample application demonstrating usage
3. **Windows App Runtime** - Bundled as MSIX dependency

### Report Rendering

- Reports are rendered as PDF using Microsoft.ReportViewer.NETCore
- PDF is displayed in a WebView control
- Supports all standard RDLC features from WinForms version

### Dependencies Included

The MSIX package automatically includes:
- Windows App Runtime 1.5 (x86 and x64)
- All required .NET libraries
- Report rendering engines
- PDF generation components

## Next Steps

- See [02-ASP-NET-Core-Integration.md](02-ASP-NET-Core-Integration.md) for web integration
- See [03-Blazor-Integration.md](03-Blazor-Integration.md) for Blazor usage
- Refer to the WinForms documentation for report authoring guidance
