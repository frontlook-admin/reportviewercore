using System;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    internal sealed class ReportViewerCommandRegistration : IDisposable
    {
        private ReportToolBar m_toolbar;
        private ToolStripItem m_item;

        internal ReportViewerCommandRegistration(ReportToolBar toolbar, ToolStripItem item)
        {
            m_toolbar = toolbar ?? throw new ArgumentNullException(nameof(toolbar));
            m_item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public void Dispose()
        {
            var toolbar = m_toolbar;
            var item = m_item;
            m_toolbar = null;
            m_item = null;
            toolbar?.RemoveCustomItem(item);
        }
    }
}
