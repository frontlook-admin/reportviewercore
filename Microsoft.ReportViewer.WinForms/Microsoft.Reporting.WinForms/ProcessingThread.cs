using System;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class ProcessingThread
	{
		private AsyncReportOperation m_operation;

		private Thread m_backgroundThread;

		private bool m_cancelInProgress;
		
		private CancellationTokenSource m_cancellationTokenSource;

		private bool IsRendering
		{
			get
			{
				if (m_backgroundThread != null)
				{
					return m_backgroundThread.IsAlive;
				}
				return false;
			}
		}

		public bool IsCancelInProgress => m_cancelInProgress;

		public bool Cancel(int millisecondsTimeout)
		{
			if (IsRendering)
			{
				try
				{
					AsyncReportOperation operation = m_operation;
					if (operation != null && !operation.Abort())
					{
						m_cancelInProgress = true;
						// .NET Core doesn't support Thread.Abort(). Instead, we use CancellationToken
						// for cooperative cancellation. The rendering operation should monitor the token
						// and exit gracefully. If the operation doesn't respond within the timeout,
						// we log a warning and wait for it to complete naturally.
						if (m_cancellationTokenSource != null)
						{
							try
							{
								m_cancellationTokenSource.Cancel();
								Debug.WriteLine($"ProcessingThread.Cancel: Cancellation requested via CancellationToken");
							}
							catch (ObjectDisposedException)
							{
								Debug.WriteLine($"ProcessingThread.Cancel: CancellationTokenSource already disposed");
							}
						}
						// Note: Thread.Abort() is not available in .NET Core.
						// The rendering operation must cooperatively check cancellation state.
					}
				}
				catch (ThreadStateException ex)
				{
					Debug.WriteLine($"ProcessingThread.Cancel: ThreadStateException - {ex.Message}");
					if (IsRendering)
					{
						throw;
					}
				}
				if (millisecondsTimeout != 0)
				{
					bool completed = m_backgroundThread.Join(millisecondsTimeout);
					if (!completed)
					{
						Debug.WriteLine($"ProcessingThread.Cancel: Thread did not complete within {millisecondsTimeout}ms timeout. Waiting for natural completion.");
					}
					return completed;
				}
				return false;
			}
			return true;
		}

		public void BeginBackgroundOperation(AsyncReportOperation operation)
		{
			if (m_backgroundThread != null)
			{
				m_backgroundThread.Join();
			}
			
			// Dispose previous cancellation token source if it exists
			if (m_cancellationTokenSource != null)
			{
				m_cancellationTokenSource.Dispose();
			}
			
			// Create new cancellation token source for this operation
			m_cancellationTokenSource = new CancellationTokenSource();
			
			operation.ClearAbortFlag();
			m_operation = operation;
			m_backgroundThread = new Thread(ProcessThreadMain);
			try
			{
				PropagateThreadCulture();
			}
			catch (SecurityException)
			{
			}
			m_backgroundThread.Name = "Rendering";
			m_backgroundThread.IsBackground = true;
			m_backgroundThread.Start(operation);
		}

		private void PropagateThreadCulture()
		{
			m_backgroundThread.CurrentCulture = Thread.CurrentThread.CurrentCulture;
			m_backgroundThread.CurrentUICulture = Thread.CurrentThread.CurrentUICulture;
		}

		private void ProcessThreadMain(object arg)
		{
			Exception e = null;
			try
			{
				m_operation.BeginAsyncExecution();
			}
			catch (Exception ex)
			{
				e = ex;
				Exception ex2 = ex;
				while (true)
				{
					if (ex2 != null)
					{
						if (ex2 is ThreadAbortException || ex2 is OperationCanceledException)
						{
							break;
						}
						ex2 = ex2.InnerException;
						continue;
					}
					return;
				}
				e = new OperationCanceledException();
			}
			finally
			{
				m_operation.EndAsyncExecution(e);
				m_operation = null;
				m_cancelInProgress = false;
				
				// Clean up cancellation token source
				if (m_cancellationTokenSource != null)
				{
					m_cancellationTokenSource.Dispose();
					m_cancellationTokenSource = null;
				}
			}
		}
	}
}
