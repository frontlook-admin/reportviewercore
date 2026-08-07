using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    internal sealed class ModernReportToolStripRenderer : ToolStripProfessionalRenderer
    {
        private readonly ReportViewerTheme m_theme;

        public ModernReportToolStripRenderer()
            : this(ReportViewerTheme.Light)
        {
        }

        public ModernReportToolStripRenderer(ReportViewerTheme theme)
            : base(new ProfessionalColorTable())
        {
            m_theme = theme ?? ReportViewerTheme.Light;
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(m_theme.ToolbarBackground);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(m_theme.ToolbarBorder))
            {
                e.Graphics.DrawLine(pen, e.AffectedBounds.Left, e.AffectedBounds.Bottom - 1, e.AffectedBounds.Right - 1, e.AffectedBounds.Bottom - 1);
            }
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            DrawItemBackground(e.Item, e.Graphics);
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            DrawItemBackground(e.Item, e.Graphics);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            DrawItemBackground(e.Item, e.Graphics);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(m_theme.ToolbarBackground);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int x = e.Item.Bounds.Left + (e.Item.Bounds.Width / 2);
            int top = e.Item.Bounds.Top + 7;
            int bottom = e.Item.Bounds.Bottom - 7;
            using (var pen = new Pen(m_theme.ToolbarBorder))
            {
                e.Graphics.DrawLine(pen, x, top, x, bottom);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? m_theme.Foreground : m_theme.DisabledForeground;
            base.OnRenderItemText(e);
        }

        private void DrawItemBackground(ToolStripItem item, Graphics graphics)
        {
            if (!item.Enabled || (!item.Selected && !item.Pressed))
            {
                return;
            }

            Rectangle bounds = item.Bounds;
            bounds.Inflate(-1, -1);
            if (bounds.Width < 4 || bounds.Height < 4)
            {
                return;
            }

            Color fillColor = item.Pressed ? Blend(m_theme.Accent, m_theme.ToolbarBackground, 0.18f) : Blend(m_theme.Accent, m_theme.ToolbarBackground, 0.08f);
            using (var path = CreateRoundedRectangle(bounds, 4))
            using (var brush = new SolidBrush(fillColor))
            using (var pen = new Pen(item.Pressed ? m_theme.Accent : m_theme.ToolbarBorder))
            {
                SmoothingMode previousSmoothingMode = graphics.SmoothingMode;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
                graphics.SmoothingMode = previousSmoothingMode;
            }
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Blend(Color foreground, Color background, float foregroundWeight)
        {
            float backgroundWeight = 1f - foregroundWeight;
            return Color.FromArgb(
                (int)(foreground.R * foregroundWeight + background.R * backgroundWeight),
                (int)(foreground.G * foregroundWeight + background.G * backgroundWeight),
                (int)(foreground.B * foregroundWeight + background.B * backgroundWeight));
        }
    }
}
