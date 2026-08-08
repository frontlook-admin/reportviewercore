using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
	internal class AsyncWaitMessage : UserControl
	{
		private LoadingCardPanel m_card;
		private TableLayoutPanel m_content;
		private TableLayoutPanel m_header;
		private TableLayoutPanel m_progressRow;
		private PictureBox m_logo;
		private LoadingIndicator m_indicator;
		private LoadingProgressBar m_progressBar;
		private Label m_loadingLabel;
		private Label m_brandLabel;
		private bool m_loadingTextCustomized;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override Font Font
		{
			get
			{
				return m_loadingLabel == null ? base.Font : m_loadingLabel.Font;
			}
			set
			{
				base.Font = value;
				if (m_loadingLabel != null)
				{
					m_loadingLabel.Font = value;
				}
			}
		}

		internal string LoadingMessage => m_loadingLabel?.Text;

		public AsyncWaitMessage()
		{
			InitializeComponent();
		}

		public void CenterToParent()
		{
			if (Parent != null)
			{
				Left = Math.Max(0, Parent.Width / 2 - Width / 2);
				Top = Math.Max(0, Parent.Height / 2 - Height / 2);
			}
		}

		internal void SetLoadingMessage(string text, bool custom)
		{
			m_loadingTextCustomized = custom;
			m_loadingLabel.Text = string.IsNullOrWhiteSpace(text) ? "Loading..." : text;
		}

		internal void SetBranding(string text, bool visible, Image logo)
		{
			m_brandLabel.Text = text ?? string.Empty;
			m_brandLabel.Visible = visible && !string.IsNullOrWhiteSpace(text);
			m_logo.Image = logo;
			m_logo.Visible = visible && logo != null;
			m_header.ColumnStyles[0].Width = m_logo.Visible ? 46 : 0;
			m_header.PerformLayout();
			m_content.PerformLayout();
		}

		internal void ApplyTheme(ReportViewerTheme theme)
		{
			BackColor = theme.CanvasBackground;
			m_card.BackColor = theme.InputBackground;
			m_card.BorderColor = theme.ToolbarBorder;
			m_loadingLabel.ForeColor = theme.Foreground;
			m_brandLabel.ForeColor = theme.DisabledForeground;
			// ToolbarBorder is intentionally subtle in dark mode and is too close
			// to the loading card background for the progress track to stand out.
			// PageBorder provides the contrast needed by both loading indicators.
			m_progressBar.ApplyTheme(theme.Accent, theme.PageBorder);
			m_indicator.ApplyTheme(theme.Accent, theme.PageBorder);
			Invalidate(true);
		}

		internal void ApplyCustomResources()
		{
			if (m_loadingTextCustomized)
			{
				return;
			}

			string progressText = LocalizationHelper.Current.ProgressText;
			if (progressText != null)
			{
				m_loadingLabel.Text = progressText;
			}
		}

		private void InitializeComponent()
		{
			m_card = new LoadingCardPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(20),
				BackColor = Color.White,
				BorderColor = Color.FromArgb(221, 226, 232)
			};

			m_content = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				ColumnCount = 1,
				RowCount = 3,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			m_content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
			m_content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
			m_content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

			m_header = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				ColumnCount = 2,
				RowCount = 1,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			m_header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
			m_header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			m_logo = new PictureBox
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				SizeMode = PictureBoxSizeMode.Zoom,
				Margin = new Padding(0, 2, 10, 2),
				Visible = false
			};

			m_loadingLabel = new Label
			{
				Dock = DockStyle.Fill,
				AutoSize = false,
				BackColor = Color.Transparent,
				Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold),
				ForeColor = Color.FromArgb(38, 46, 56),
				Text = "Loading...",
				TextAlign = ContentAlignment.MiddleLeft,
				UseCompatibleTextRendering = false
			};

			m_header.Controls.Add(m_logo, 0, 0);
			m_header.Controls.Add(m_loadingLabel, 1, 0);

			m_progressRow = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				BackColor = Color.Transparent,
				ColumnCount = 2,
				RowCount = 1,
				Margin = Padding.Empty,
				Padding = Padding.Empty
			};
			m_progressRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));
			m_progressRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

			m_indicator = new LoadingIndicator
			{
				Anchor = AnchorStyles.None,
				Margin = Padding.Empty
			};
			m_progressBar = new LoadingProgressBar
			{
				Dock = DockStyle.Fill,
				Height = 6,
				Margin = new Padding(8, 11, 0, 11),
				TabStop = false
			};
			m_progressRow.Controls.Add(m_indicator, 0, 0);
			m_progressRow.Controls.Add(m_progressBar, 1, 0);

			m_brandLabel = new Label
			{
				Dock = DockStyle.Fill,
				AutoSize = false,
				BackColor = Color.Transparent,
				Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
				ForeColor = Color.FromArgb(157, 165, 174),
				TextAlign = ContentAlignment.BottomCenter,
				UseCompatibleTextRendering = false,
				Visible = false
			};

			m_content.Controls.Add(m_header, 0, 0);
			m_content.Controls.Add(m_progressRow, 0, 1);
			m_content.Controls.Add(m_brandLabel, 0, 2);
			m_card.Controls.Add(m_content);
			Controls.Add(m_card);
			BackColor = Color.White;
			MinimumSize = new Size(280, 150);
			Size = new Size(360, 174);
			Name = "AsyncWaitMessage";
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		}

		private sealed class LoadingCardPanel : Panel
		{
			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			internal Color BorderColor { get; set; }

			public LoadingCardPanel()
			{
				SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
				BorderColor = Color.FromArgb(221, 226, 232);
			}

			protected override void OnPaintBackground(PaintEventArgs e)
			{
				if (Parent != null)
				{
					e.Graphics.Clear(Parent.BackColor);
				}
			}

			protected override void OnPaint(PaintEventArgs e)
			{
				base.OnPaint(e);
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
				using (var path = CreateRoundRectangle(bounds, 10))
				using (var fill = new SolidBrush(BackColor))
				using (var border = new Pen(BorderColor, 1f))
				{
					e.Graphics.FillPath(fill, path);
					e.Graphics.DrawPath(border, path);
				}
			}

			protected override void OnSizeChanged(EventArgs e)
			{
				base.OnSizeChanged(e);
				using (var path = CreateRoundRectangle(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), 10))
				{
					Region = new Region(path);
				}
			}

			private static GraphicsPath CreateRoundRectangle(Rectangle bounds, int radius)
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
}
