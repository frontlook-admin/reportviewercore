using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Microsoft.Reporting.WinForms
{
    /// <summary>
    /// Branding helpers for applications hosting the WinForms report viewer.
    /// </summary>
    public static class ReportViewerBranding
    {
        private const int IconSize = 32;

        /// <summary>
        /// Creates the report viewer application icon: a report page with data bars.
        /// The returned icon is owned by the caller.
        /// </summary>
        public static Icon CreateApplicationIcon()
        {
            using var bitmap = new Bitmap(IconSize, IconSize, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                using (var backgroundPath = CreateRoundedRectangle(1, 1, 30, 30, 7))
                using (var backgroundBrush = new SolidBrush(Color.FromArgb(29, 78, 121)))
                using (var backgroundPen = new Pen(Color.FromArgb(14, 47, 75), 1))
                {
                    graphics.FillPath(backgroundBrush, backgroundPath);
                    graphics.DrawPath(backgroundPen, backgroundPath);
                }

                var page = new[]
                {
                    new Point(8, 5),
                    new Point(20, 5),
                    new Point(25, 10),
                    new Point(25, 27),
                    new Point(8, 27)
                };

                using (var pageBrush = new SolidBrush(Color.FromArgb(248, 250, 252)))
                using (var pagePen = new Pen(Color.FromArgb(203, 213, 225), 1))
                {
                    graphics.FillPolygon(pageBrush, page);
                    graphics.DrawPolygon(pagePen, page);
                }

                using (var foldBrush = new SolidBrush(Color.FromArgb(191, 219, 254)))
                using (var foldPen = new Pen(Color.FromArgb(96, 165, 250), 1))
                {
                    var fold = new[]
                    {
                        new Point(20, 5),
                        new Point(20, 10),
                        new Point(25, 10)
                    };
                    graphics.FillPolygon(foldBrush, fold);
                    graphics.DrawLines(foldPen, fold);
                }

                using (var linePen = new Pen(Color.FromArgb(37, 99, 235), 1.25f))
                {
                    graphics.DrawLine(linePen, 11, 13, 22, 13);
                    graphics.DrawLine(linePen, 11, 16, 22, 16);
                }

                using (var tealBrush = new SolidBrush(Color.FromArgb(20, 184, 166)))
                using (var amberBrush = new SolidBrush(Color.FromArgb(245, 158, 11)))
                {
                    graphics.FillRectangle(tealBrush, 11, 23, 2, 2);
                    graphics.FillRectangle(tealBrush, 15, 20, 2, 5);
                    graphics.FillRectangle(amberBrush, 19, 18, 2, 7);
                    graphics.FillRectangle(amberBrush, 22, 21, 2, 4);
                }
            }

            IntPtr iconHandle = bitmap.GetHicon();
            try
            {
                using var icon = Icon.FromHandle(iconHandle);
                return (Icon)icon.Clone();
            }
            finally
            {
                DestroyIcon(iconHandle);
            }
        }

        private static GraphicsPath CreateRoundedRectangle(int x, int y, int width, int height, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(x, y, diameter, diameter, 180, 90);
            path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
            path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
            path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
