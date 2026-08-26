using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Reporting.WinForms
{
	[TypeConverter(typeof(TypeNameHidingExpandableObjectConverter))]
	public abstract class Report
	{
		private string m_displayName = "";

		private int m_drillthroughDepth = 1;

		internal object m_syncObject = new object();

		[SRDescription("DisplayNameDesc")]
		[NotifyParentProperty(true)]
		[DefaultValue("")]
		public string DisplayName
		{
			get
			{
				return m_displayName;
			}
			set
			{
				m_displayName = value;
			}
		}

		internal abstract string DisplayNameForUse
		{
			get;
		}

		[Browsable(false)]
		public bool IsDrillthroughReport => DrillthroughDepth > 1;

		internal int DrillthroughDepth
		{
			get
			{
				return m_drillthroughDepth;
			}
			set
			{
				m_drillthroughDepth = value;
			}
		}

		internal abstract bool IsReadyForConnection
		{
			get;
		}

		[Browsable(false)]
		public bool IsReadyForRendering
		{
			get
			{
				try
				{
					return PrepareForRender();
				}
				catch
				{
					return false;
				}
			}
		}

		internal abstract bool IsPreparedReportReadyForRendering
		{
			get;
		}

		internal abstract bool HasDocMap
		{
			get;
		}

		internal abstract int AutoRefreshInterval
		{
			get;
		}

		internal abstract bool CanSelfCancel
		{
			get;
		}

		internal event EventHandler<ReportChangedEventArgs> Change;

		internal Report()
		{
		}

		public abstract ReportParameterInfoCollection GetParameters();

		internal abstract ParametersPaneLayout GetParametersPaneLayout();

		public abstract void SetParameters(IEnumerable<ReportParameter> parameters);

		public abstract int GetTotalPages(out PageCountMode pageCountMode);

		public abstract RenderingExtension[] ListRenderingExtensions();

		public abstract void LoadReportDefinition(TextReader report);

		public abstract void Refresh();

		public abstract byte[] Render(string format, string deviceInfo, PageCountMode pageCountMode, out string mimeType, out string encoding, out string fileNameExtension, out string[] streams, out Warning[] warnings);

		internal abstract byte[] InternalRenderStream(string format, string streamID, string deviceInfo, out string mimeType, out string encoding);

		internal abstract void InternalDeliverReportItem(string format, string deviceInfo, ExtensionSettings settings, string description, string eventType, string matchData);

		internal abstract int PerformSearch(string searchText, int startPage, int endPage);

		internal abstract void PerformToggle(string toggleId);

		internal abstract int PerformBookmarkNavigation(string bookmarkId, out string uniqueName);

		internal abstract int PerformDocumentMapNavigation(string documentMapId);

		public abstract ReportPageSettings GetDefaultPageSettings();

		public int GetTotalPages()
		{
			PageCountMode pageCountMode;
			return GetTotalPages(out pageCountMode);
		}

        public Task<byte[]> RenderAsync(string format)
		{
			return RenderAsync(format, null, PageCountMode.Estimate, CancellationToken.None, null);
		}

		public Task<byte[]> RenderAsync(string format, CancellationToken cancellationToken)
		{
			return RenderAsync(format, null, PageCountMode.Estimate, cancellationToken, null);
		}

		public async Task<byte[]> RenderAsync(string format, string deviceInfo, PageCountMode pageCountMode, CancellationToken cancellationToken = default, IProgress<ReportRenderProgress> progress = null)
		{
			if (format == null)
			{
				throw new ArgumentNullException(nameof(format));
			}

			var stopwatch = Stopwatch.StartNew();
			cancellationToken.ThrowIfCancellationRequested();
			ReportProgress(progress, new ReportRenderProgress(ReportRenderStage.Started, format, stopwatch.Elapsed, null, null, "Report rendering started."));

			using (cancellationToken.Register(CancelRender))
			{
				try
				{
					ReportProgress(progress, new ReportRenderProgress(ReportRenderStage.Rendering, format, stopwatch.Elapsed, null, null, "Report rendering is in progress."));
					byte[] result = await Task.Run(() =>
					{
						cancellationToken.ThrowIfCancellationRequested();
						string mimeType;
						string encoding;
						string fileNameExtension;
						string[] streams;
						Warning[] warnings;
						return Render(format, deviceInfo, pageCountMode, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);
					}, CancellationToken.None).ConfigureAwait(false);

					cancellationToken.ThrowIfCancellationRequested();
					ReportProgress(progress, new ReportRenderProgress(ReportRenderStage.Completed, format, stopwatch.Elapsed, result?.LongLength ?? 0L, null, "Report rendering completed."));
					return result;
				}
				catch (OperationCanceledException ex)
				{
					ReportProgress(progress, new ReportRenderProgress(ReportRenderStage.Cancelled, format, stopwatch.Elapsed, null, ex, "Report rendering was cancelled."));
					throw;
				}
				catch (Exception ex) when (cancellationToken.IsCancellationRequested)
				{
					var cancellation = new OperationCanceledException("Report rendering was cancelled.", ex, cancellationToken);
					ReportProgress(progress, new ReportRenderProgress(ReportRenderStage.Cancelled, format, stopwatch.Elapsed, null, cancellation, "Report rendering was cancelled."));
					throw cancellation;
				}
				catch (Exception ex)
				{
					ReportProgress(progress, new ReportRenderProgress(ReportRenderStage.Failed, format, stopwatch.Elapsed, null, ex, "Report rendering failed."));
					throw;
				}
				finally
				{
					ClearCancelState();
				}
			}
		}

		private void CancelRender()
		{
			try
			{
				if (CanSelfCancel)
				{
					SetCancelState(shouldCancel: true);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Report.RenderAsync cancellation: {ex.GetType().Name} - {ex.Message}");
			}
		}

		private void ClearCancelState()
		{
			try
			{
				if (CanSelfCancel)
				{
					SetCancelState(shouldCancel: false);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Report.RenderAsync clear cancellation: {ex.GetType().Name} - {ex.Message}");
			}
		}

		private static void ReportProgress(IProgress<ReportRenderProgress> progress, ReportRenderProgress value)
		{
			if (progress == null)
			{
				return;
			}

			try
			{
				progress.Report(value);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Report.RenderAsync progress callback: {ex.GetType().Name} - {ex.Message}");
			}
		}

		public byte[] Render(string format)
		{
			return Render(format, null);
		}

		public byte[] Render(string format, string deviceInfo)
		{
			string mimeType;
			string encoding;
			string fileNameExtension;
			string[] streams;
			Warning[] warnings;
			return Render(format, deviceInfo, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);
		}

		public byte[] Render(string format, string deviceInfo, out string mimeType, out string encoding, out string fileNameExtension, out string[] streams, out Warning[] warnings)
		{
			return Render(format, deviceInfo, PageCountMode.Estimate, out mimeType, out encoding, out fileNameExtension, out streams, out warnings);
		}

        public Task<ReportExportResult> RenderToStreamAsync(
            Stream destination,
            string format,
            string deviceInfo = null,
            CancellationToken cancellationToken = default,
            IProgress<ReportRenderProgress> progress = null)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (!destination.CanWrite)
            {
                throw new ArgumentException("The destination stream must be writable.", nameof(destination));
            }
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentException("A render format is required.", nameof(format));
            }

            return RenderToStreamAsyncCore(destination, format, deviceInfo, cancellationToken);
        }

        private async Task<ReportExportResult> RenderToStreamAsyncCore(
            Stream destination,
            string format,
            string deviceInfo,
            CancellationToken cancellationToken)
        {
            var operation = new AsyncExportOperation(this, PageCountMode.Actual, format, deviceInfo ?? string.Empty, allowInternalRenderers: false, null);
            try
            {
                using (cancellationToken.Register(() => operation.Abort()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Run(operation.BeginAsyncExecution, CancellationToken.None).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                using (FileStream source = File.OpenRead(operation.OutputPath))
                {
                    await source.CopyToAsync(destination, 64 * 1024, cancellationToken).ConfigureAwait(false);
                }

                return new ReportExportResult(format, null, new FileInfo(operation.OutputPath).Length);
            }
            finally
            {
                operation.Cleanup();
            }
        }

		public DocumentMapNode GetDocumentMap()
		{
			return GetDocumentMap(DisplayNameForUse);
		}

		internal abstract Report PerformDrillthrough(string drillthroughId, out string reportPath);

		internal abstract int PerformSort(string sortId, SortOrder sortDirection, bool clearSort, PageCountMode pageCountMode, out string uniqueName);

		internal bool PrepareForRender()
		{
			lock (m_syncObject)
			{
				if (IsReadyForConnection)
				{
					EnsureExecutionSession();
					return IsPreparedReportReadyForRendering;
				}
				return false;
			}
		}

		internal abstract void EnsureExecutionSession();

		internal abstract DocumentMapNode GetDocumentMap(string rootLabel);

		internal abstract void SetCancelState(bool shouldCancel);

		public void LoadReportDefinition(Stream report)
		{
			if (report == null)
			{
				throw new ArgumentNullException("report");
			}
			LoadReportDefinition(new StreamReader(report));
		}

		internal void OnChange(bool isRefreshOnly)
		{
			if (this.Change != null)
			{
				this.Change(this, new ReportChangedEventArgs(isRefreshOnly));
			}
		}

		internal void OnChange(object sender, EventArgs e)
		{
			OnChange(isRefreshOnly: false);
		}

		public void SetParameters(ReportParameter parameter)
		{
			SetParameters(new ReportParameter[1]
			{
				parameter
			});
		}
	}
}
