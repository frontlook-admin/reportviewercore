using System;
using System.Drawing;

namespace Microsoft.Reporting.WinForms
{
    /// <summary>Owns a rendered preview thumbnail and its page identity.</summary>
    public sealed class ReportPageThumbnail : IDisposable
    {
        private bool _disposed;

        public ReportPageThumbnail(int page, Image image)
        {
            if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
            Image = image ?? throw new ArgumentNullException(nameof(image));
            Page = page;
        }

        public int Page { get; private set; }
        public Image Image { get; private set; }
        public Size Size { get { return Image.Size; } }
        public bool IsDisposed { get { return _disposed; } }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Image.Dispose();
            Image = null;
            GC.SuppressFinalize(this);
        }
    }
}
