using System;
using System.Collections.Generic;
using System.Drawing;

namespace Microsoft.Reporting.WinForms
{
    /// <summary>Shared zoom and page geometry calculations for the preview surface.</summary>
    public static class ReportPageLayout
    {
        public static float CalculateZoom(ZoomMode mode, SizeF viewportPixels, SizeF pageMillimetres, float dpiX, float dpiY, int padding = 0, int percent = 100)
        {
            if (viewportPixels.Width <= 0 || viewportPixels.Height <= 0) throw new ArgumentOutOfRangeException(nameof(viewportPixels));
            if (pageMillimetres.Width <= 0 || pageMillimetres.Height <= 0) throw new ArgumentOutOfRangeException(nameof(pageMillimetres));
            if (dpiX <= 0 || dpiY <= 0) throw new ArgumentOutOfRangeException(nameof(dpiX));
            if (padding < 0 || percent <= 0) throw new ArgumentOutOfRangeException(nameof(percent));

            SizeF pagePixels = MillimetresToPixels(pageMillimetres, dpiX, dpiY);
            switch (mode)
            {
                case ZoomMode.ActualSize:
                    return Math.Min(dpiX, dpiY) / 96f;
                case ZoomMode.PageWidth:
                    return Math.Max(0.01f, (viewportPixels.Width - padding) / pagePixels.Width);
                case ZoomMode.FullPage:
                    return Math.Max(0.01f, Math.Min((viewportPixels.Width - padding) / pagePixels.Width, (viewportPixels.Height - padding) / pagePixels.Height));
                default:
                    return Math.Max(0.01f, percent / 100f);
            }
        }

        public static RectangleF GetPageBounds(int pageIndex, SizeF pagePixels, float zoom, float gap = 12)
        {
            if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
            if (pagePixels.Width <= 0 || pagePixels.Height <= 0 || zoom <= 0) throw new ArgumentOutOfRangeException(nameof(pagePixels));
            return new RectangleF(0, pageIndex * (pagePixels.Height * zoom + gap), pagePixels.Width * zoom, pagePixels.Height * zoom);
        }

        private static SizeF MillimetresToPixels(SizeF millimetres, float dpiX, float dpiY)
        {
            return new SizeF(millimetres.Width / 25.4f * dpiX, millimetres.Height / 25.4f * dpiY);
        }
    }

    /// <summary>Deterministic page stack geometry for continuous vertical scrolling.</summary>
    public sealed class ContinuousScrollLayout
    {
        private readonly float _gap;
        private float[] _heights = new float[0];

        public ContinuousScrollLayout(float gap = 12)
        {
            if (gap < 0) throw new ArgumentOutOfRangeException(nameof(gap));
            _gap = gap;
        }

        public float TotalHeight
        {
            get
            {
                if (_heights.Length == 0) return 0;
                float total = _gap * (_heights.Length - 1);
                foreach (float height in _heights) total += height;
                return total;
            }
        }

        public int PageCount { get { return _heights.Length; } }

        public void SetPageHeights(IEnumerable<float> heights)
        {
            if (heights == null) throw new ArgumentNullException(nameof(heights));
            var next = new List<float>();
            foreach (float height in heights)
            {
                if (height <= 0) throw new ArgumentOutOfRangeException(nameof(heights));
                next.Add(height);
            }
            _heights = next.ToArray();
        }

        public float GetPageTop(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _heights.Length) throw new ArgumentOutOfRangeException(nameof(pageIndex));
            float top = 0;
            for (int i = 0; i < pageIndex; i++) top += _heights[i] + _gap;
            return top;
        }

        public int HitTest(float y)
        {
            if (y < 0) return -1;
            for (int i = 0; i < _heights.Length; i++)
            {
                float top = GetPageTop(i);
                if (y >= top && y < top + _heights[i]) return i;
            }
            return -1;
        }
    }
}
