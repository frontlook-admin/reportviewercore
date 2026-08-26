using System;
using System.Linq;
using System.Windows.Forms;
using FluentAssertions;
using Microsoft.Reporting.WinForms;
using ReportViewerControl = Microsoft.Reporting.WinForms.ReportViewer;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportViewerAccessibilityKeyboardTests
{
    [Fact]
    public void Viewer_exposes_accessible_name_and_configurable_default_shortcuts()
    {
        using var viewer = new ReportViewerControl();

        viewer.AccessibleName.Should().NotBeNullOrWhiteSpace();
        viewer.AccessibleRole.Should().Be(AccessibleRole.Pane);
        viewer.TabStop.Should().BeTrue();
        viewer.RestoreFocusAfterOperation.Should().BeTrue();
        viewer.KeyboardShortcuts.Should().Contain(shortcut =>
            shortcut.Command == ReportViewerKeyboardCommand.Refresh && shortcut.Keys == Keys.F5);
        viewer.KeyboardShortcuts.Should().Contain(shortcut =>
            shortcut.Command == ReportViewerKeyboardCommand.Find && shortcut.Keys == (Keys.Control | Keys.F));
    }

    [Fact]
    public void Custom_shortcut_resolves_to_command_without_color_or_theme_state()
    {
        var shortcuts = new ReportViewerKeyboardShortcutCollection
        {
            new ReportViewerKeyboardShortcut(ReportViewerKeyboardCommand.Refresh, Keys.Control | Keys.R)
        };

        shortcuts.TryGetCommand(Keys.Control | Keys.R, out var command).Should().BeTrue();
        command.Should().Be(ReportViewerKeyboardCommand.Refresh);
        shortcuts.TryGetCommand(Keys.Control | Keys.P, out _).Should().BeFalse();
    }

    [Fact]
    public void High_contrast_theme_uses_explicit_contrast_colors_and_selection_is_not_color_only()
    {
        using var viewer = new ReportViewerControl { Theme = ReportViewerTheme.HighContrast };

        viewer.Theme.ToolbarBackground.Should().Be(System.Drawing.Color.Black);
        viewer.Theme.Foreground.Should().Be(System.Drawing.Color.White);
        viewer.Theme.ErrorForeground.Should().NotBe(viewer.Theme.ErrorBackground);
        typeof(ReportViewerRowSelectionChangedEventArgs).GetProperty(nameof(ReportViewerRowSelectionChangedEventArgs.Selection))
            .Should().NotBeNull();
    }
}
