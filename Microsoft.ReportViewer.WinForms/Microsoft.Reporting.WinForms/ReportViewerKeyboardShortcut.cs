using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    public enum ReportViewerKeyboardCommand
    {
        Refresh,
        Print,
        DirectPrint,
        Find,
        ClearSearch,
        Cancel,
        FirstPage,
        PreviousPage,
        NextPage,
        LastPage,
        ZoomIn,
        ZoomOut,
        ResetZoom,
        PreviousAction,
        NextAction
    }

    public sealed class ReportViewerKeyboardShortcut
    {
        public ReportViewerKeyboardCommand Command { get; }
        public Keys Keys { get; }

        public ReportViewerKeyboardShortcut(ReportViewerKeyboardCommand command, Keys keys)
        {
            if ((keys & Keys.KeyCode) == Keys.None)
            {
                throw new ArgumentException("A shortcut must include a key.", nameof(keys));
            }

            Command = command;
            Keys = keys;
        }
    }

    public sealed class ReportViewerKeyboardShortcutCollection : Collection<ReportViewerKeyboardShortcut>
    {
        public bool TryGetCommand(Keys keys, out ReportViewerKeyboardCommand command)
        {
            var shortcut = this.FirstOrDefault(item => item.Keys == keys);
            if (shortcut == null)
            {
                command = default;
                return false;
            }

            command = shortcut.Command;
            return true;
        }
    }
}
