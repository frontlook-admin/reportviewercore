# Microsoft.ReportViewer.WinForms.Tests

Comprehensive unit test suite for the Microsoft.ReportViewer.WinForms library with a focus on resource management, thread safety, and API correctness.

## Test Coverage Goals

Target: **80% code coverage** on modified/new code

## Test Projects

### PrintDialogSettingsTests
Tests the new `PrintDialogSettings` class ensuring:
- Proper initialization and property management
- No resource leaks (no disposal required)
- Correct round-trip conversion from/to PrintDialog
- Null safety and argument validation

### CustomPrintDialogTests
Tests the `CustomPrintDialog` class focusing on:
- Constructor validation (ArgumentNullException guards)
- Obsolete `GetPrintDialog()` still works but requires disposal
- New `GetPrintDialogSettings()` method returns leak-free settings
- JSON serialization/deserialization
- Exception handling in margin calculations

### ReportViewerRefactoringTests
Tests the refactored `SetPrinterAndPageSettings()` method:
- Helper methods work independently
- File I/O error handling
- Settings application to PrintDialog
- End-to-end workflow validation

## Running Tests

### Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Click "Run All Tests"

### Command Line
```bash
dotnet test
```

### With Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Test Framework

- **xUnit**: Primary test framework
- **FluentAssertions**: Readable assertion syntax
- **Moq**: Mocking framework for dependencies
- **Coverlet**: Code coverage reporting

## Test Categories

### Resource Management Tests
Verify that resources (PrintDialog, Streams, etc.) are properly disposed or don't require disposal.

### Null Safety Tests
Ensure ArgumentNullException is thrown for null parameters where appropriate.

### Exception Handling Tests
Verify that exceptions are logged and handled gracefully without silent failures.

### Integration Tests
Test complete workflows to ensure refactored code maintains original functionality.

## Code Coverage Report

Generate HTML coverage report:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
reportgenerator -reports:coverage.opencover.xml -targetdir:coveragereport
```

## Contributing

When adding new features to Microsoft.ReportViewer.WinForms:
1. Add corresponding tests to this project
2. Aim for 80% coverage on new code
3. Test both happy path and error conditions
4. Include null safety tests
5. Verify resource disposal

## Test Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `Constructor_WithNullParameter_ThrowsArgumentNullException`
- `GetPrintDialogSettings_ReturnsValidSettings`
- `ApplyTo_WithValidPrintDialog_AppliesAllSettings`

## Important Notes

### Windows Forms Dependency
Many tests require Windows Forms components which may have limitations in headless environments. These tests are designed to run on Windows with a UI subsystem available.

### Print Dialog Tests
Tests involving PrintDialog creation are wrapped in `using` statements to ensure proper disposal, demonstrating best practices for API usage.

### Obsolete API Tests
Tests for `GetPrintDialog()` verify the method still works but is marked obsolete. These tests also demonstrate the proper disposal pattern required when using this deprecated method.
