using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
	internal class AsyncWaitControl : AlphaPanel, IDisposable
	{
		private AsyncWaitMessage m_waitMessage;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override Font Font
		{
			get
			{
				return m_waitMessage.Font;
			}
			set
			{
				m_waitMessage.Font = value;
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override Color BackColor
		{
			get
			{
				if (m_waitMessage != null)
				{
					return m_waitMessage.BackColor;
				}
				return Color.Empty;
			}
			set
			{
				if (m_waitMessage != null)
				{
					m_waitMessage.BackColor = value;
				}
			}
		}

		public AsyncWaitControl(IRenderable renderable)
			: base(renderable)
		{
			InitializeComponent();
		}

		internal void SetLoadingMessage(string text, bool custom)
		{
			m_waitMessage.SetLoadingMessage(text, custom);
		}

		internal void SetBranding(string text, bool visible, Image logo)
		{
			m_waitMessage.SetBranding(text, visible, logo);
		}

		internal void ApplyTheme(ReportViewerTheme theme)
		{
			base.ApplyTheme(theme.CanvasBackground);
			m_waitMessage.ApplyTheme(theme);
		}

		private void InitializeComponent()
		{
			AutoSize = false;
			m_waitMessage = new Microsoft.Reporting.WinForms.AsyncWaitMessage();
			m_waitMessage.AutoSize = false;
			m_waitMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			m_waitMessage.BackColor = System.Drawing.Color.White;
			base.Controls.Add(m_waitMessage);
			m_waitMessage.CenterToParent();
			Cursor = System.Windows.Forms.Cursors.WaitCursor;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			m_waitMessage.CenterToParent();
		}

		internal void ApplyCustomResources()
		{
			m_waitMessage.ApplyCustomResources();
		}
	}
}
