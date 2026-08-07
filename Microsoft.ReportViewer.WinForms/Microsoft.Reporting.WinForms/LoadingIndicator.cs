using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class LoadingIndicator : Control
	{
		private readonly Timer m_timer;
		private int m_startAngle;
		private Color m_accentColor = Color.FromArgb(35, 113, 194);
		private Color m_trackColor = Color.FromArgb(221, 226, 232);

		public LoadingIndicator()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
			Size = new Size(26, 26);
			TabStop = false;
			AccessibleName = "Report loading";
			AccessibleRole = AccessibleRole.ProgressBar;

			m_timer = new Timer
			{
				Interval = 70
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
			Rectangle bounds = new Rectangle(3, 3, Math.Max(1, Width - 6), Math.Max(1, Height - 6));
			using (var trackPen = new Pen(m_trackColor, 3f))
			using (var accentPen = new Pen(m_accentColor, 3f))
			{
				trackPen.StartCap = LineCap.Round;
				trackPen.EndCap = LineCap.Round;
				accentPen.StartCap = LineCap.Round;
				accentPen.EndCap = LineCap.Round;
				e.Graphics.DrawArc(trackPen, bounds, 0, 360);
				e.Graphics.DrawArc(accentPen, bounds, m_startAngle, 105);
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
			m_startAngle = (m_startAngle + 24) % 360;
			Invalidate();
		}
	}
}
