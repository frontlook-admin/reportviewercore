using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
    internal sealed class ModernReportToolStripRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color ToolbarBackground = Color.FromArgb(248, 250, 252);
        private static readonly Color ToolbarBorder = Color.FromArgb(221, 226, 232);
        private static readonly Color HoverBackground = Color.FromArgb(229, 238, 250);
        private static readonly Color PressedBackground = Color.FromArgb(207, 224, 245);
        private static readonly Color Accent = Color.FromArgb(35, 113, 194);
        private static readonly Color Separator = Color.FromArgb(211, 218, 226);
        private static readonly Color Foreground = Color.FromArgb(38, 46, 56);
        private static readonly Color DisabledForeground = Color.FromArgb(157, 165, 174);

        public ModernReportToolStripRenderer()
            : base(new ProfessionalColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(ToolbarBackground);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(ToolbarBorder))
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

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int x = e.Item.Bounds.Left + (e.Item.Bounds.Width / 2);
            int top = e.Item.Bounds.Top + 7;
            int bottom = e.Item.Bounds.Bottom - 7;
            using (var pen = new Pen(Separator))
            {
                e.Graphics.DrawLine(pen, x, top, x, bottom);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Foreground : DisabledForeground;
            base.OnRenderItemText(e);
        }

        private static void DrawItemBackground(ToolStripItem item, Graphics graphics)
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

            Color fillColor = item.Pressed ? PressedBackground : HoverBackground;
            using (var path = CreateRoundedRectangle(bounds, 4))
            using (var brush = new SolidBrush(fillColor))
            using (var pen = new Pen(item.Pressed ? Accent : ToolbarBorder))
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
    }
}
