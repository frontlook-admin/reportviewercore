using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
	internal class AlphaPanel : Panel
	{
		private IRenderable m_background;

		private double m_opacity = 0.5;

		private Brush m_alphaFillBrush;

		private Color m_overlayColor = Color.White;

		public AlphaPanel(IRenderable background)
		{
			if (background == null)
			{
				throw new ArgumentNullException("background");
			}
			m_background = background;
			SetStyle(ControlStyles.AllPaintingInWmPaint, value: true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, value: true);
			SetStyle(ControlStyles.ResizeRedraw, value: true);
			SetStyle(ControlStyles.UserPaint, value: true);
			SetAlphaFillBrush();
		}

		private void SetAlphaFillBrush()
		{
			if (m_alphaFillBrush != null)
			{
				m_alphaFillBrush.Dispose();
			}
			m_alphaFillBrush = new SolidBrush(Color.FromArgb(Convert.ToInt32(m_opacity * 255.0), m_overlayColor));
		}

		internal void ApplyTheme(Color overlayColor)
		{
			m_overlayColor = overlayColor;
			SetAlphaFillBrush();
			Invalidate();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_alphaFillBrush.Dispose();
			}
			base.Dispose(disposing);
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			using (var brush = new SolidBrush(m_overlayColor))
			{
				e.Graphics.FillRectangle(brush, e.ClipRectangle);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_background.CanRender)
			{
				e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
				e.Graphics.CompositingMode = CompositingMode.SourceOver;
				try
				{
					m_background.RenderToGraphics(e.Graphics);
				}
				catch
				{
					using (var brush = new SolidBrush(m_overlayColor))
					{
						e.Graphics.FillRectangle(brush, base.ClientRectangle);
					}
					return;
				}
				e.Graphics.FillRectangle(m_alphaFillBrush, base.ClientRectangle);
			}
		}
	}
}
