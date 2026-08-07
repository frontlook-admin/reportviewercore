using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class LoadingProgressBar : Control
	{
		private readonly Timer m_timer;
		private int m_offset;
		private Color m_accentColor = Color.FromArgb(35, 113, 194);
		private Color m_trackColor = Color.FromArgb(221, 226, 232);

		public LoadingProgressBar()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
			Height = 8;
			TabStop = false;

			m_timer = new Timer
			{
				Interval = 45
			};
			m_timer.Tick += OnTimerTick;
		}

		internal void ApplyTheme(Color accentColor, Color trackColor)
		{
			m_accentColor = accentColor;
			m_trackColor = trackColor;
			Invalidate();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible)
			{
				m_timer.Start();
			}
			else
			{
				m_timer.Stop();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			Rectangle bounds = new Rectangle(0, 1, Math.Max(1, Width - 1), Math.Max(1, Height - 2));
			using (var path = CreateRoundRectangle(bounds, Math.Max(2, bounds.Height / 2)))
			using (var trackBrush = new SolidBrush(m_trackColor))
			{
				e.Graphics.FillPath(trackBrush, path);
				e.Graphics.SetClip(path);
				int segmentWidth = Math.Max(32, bounds.Width / 4);
				int x = (m_offset % (bounds.Width + segmentWidth)) - segmentWidth;
				using (var accentBrush = new SolidBrush(m_accentColor))
				{
					e.Graphics.FillRectangle(accentBrush, x, bounds.Y, segmentWidth, bounds.Height);
				}
				e.Graphics.ResetClip();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_timer.Stop();
				m_timer.Dispose();
			}
			base.Dispose(disposing);
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			m_offset = (m_offset + 8) % Math.Max(1, Width + 128);
			Invalidate();
		}

		private static GraphicsPath CreateRoundRectangle(Rectangle bounds, int radius)
		{
			int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
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
