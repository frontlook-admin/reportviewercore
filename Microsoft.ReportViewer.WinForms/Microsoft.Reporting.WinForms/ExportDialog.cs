using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Reporting.WinForms
{
	internal sealed class ExportDialog : Form
	{
		private Button cancelButton;

		private CheckBox openAfterExport;

		private Label exportLabel;

		private Container components;

		private ReportViewer m_viewerControl;

		private RenderingExtension m_format;

		private string m_deviceInfo;

		private string m_fileName;

		private AsyncExportOperation m_exportOperation;

		private bool m_cancelRequested;

		private bool m_closing;

		private static string s_lastExportDirectory;

		internal ExportDialog(ReportViewer viewer, RenderingExtension extension, string deviceInfo, string fileName)
		{
			InitializeComponent();
			Text = LocalizationHelper.Current.ExportDialogTitle;
			cancelButton.Text = LocalizationHelper.Current.ExportDialogCancelButton;
			openAfterExport.Text = ReportPreviewStrings.OpenFileAfterExport;
			exportLabel.Text = LocalizationHelper.Current.ExportDialogStatusText;
			m_viewerControl = viewer;
			m_format = extension;
			m_deviceInfo = deviceInfo;
			if (m_deviceInfo == null)
			{
				m_deviceInfo = "";
			}
			m_fileName = fileName;
			exportLabel.MinimumSize = TextRenderer.MeasureText(exportLabel.Text, exportLabel.Font);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			exportLabel = new System.Windows.Forms.Label();
			cancelButton = new System.Windows.Forms.Button();
			openAfterExport = new System.Windows.Forms.CheckBox();
			SuspendLayout();
			exportLabel.Dock = DockStyle.Top;
			exportLabel.Location = new Point(16, 8);
			exportLabel.Size = new Size(274, 24);
			exportLabel.TabIndex = 1;
			exportLabel.TextAlign = ContentAlignment.TopCenter;
			exportLabel.Name = "exportLabel";
			cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			cancelButton.AutoSize = true;
			openAfterExport.AutoSize = true;
			openAfterExport.Location = new Point(64, 45);
			openAfterExport.Name = "openAfterExport";
			openAfterExport.Size = new Size(145, 19);
			openAfterExport.TabIndex = 2;
			openAfterExport.Text = ReportPreviewStrings.OpenFileAfterExport;
			cancelButton.Location = new Point(77, 72);
			cancelButton.Size = new Size(120, 23);
			cancelButton.Name = "cancelButton";
			cancelButton.Click += new System.EventHandler(CancelButton_Click);
			AutoSize = true;
			ClientSize = new Size(274, 108);
			base.Controls.Add(cancelButton);
			base.Controls.Add(openAfterExport);
			base.Controls.Add(exportLabel);
			Cursor = System.Windows.Forms.Cursors.Default;
			base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			base.MaximizeBox = false;
			base.MinimizeBox = false;
			base.Name = "ExportDialog";
			base.ShowInTaskbar = false;
			ResumeLayout(false);
			PerformLayout();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			cancelButton.Font = Font;
			try
			{
				if (!m_viewerControl.CancelAllRenderingRequests())
				{
					throw new InvalidOperationException("The previous report rendering operation did not finish after cancellation.");
				}

				m_exportOperation = new AsyncExportOperation(m_viewerControl.Report, PageCountMode.Estimate, m_format.Name, m_deviceInfo, allowInternalRenderers: false, null);
				m_exportOperation.StartedAtUtc = DateTime.UtcNow;
				m_viewerControl.NotifyRenderingProgress(new ReportRenderProgress(ReportRenderStage.Started, m_format.Name, TimeSpan.Zero, null, null, "Report export started."));
				m_viewerControl.NotifyRenderingProgress(new ReportRenderProgress(ReportRenderStage.Rendering, m_format.Name, TimeSpan.Zero, null, null, "Report export is in progress."));
				m_exportOperation.Completed += OnExportComplete;
				m_viewerControl.BackgroundThread.BeginBackgroundOperation(m_exportOperation);
			}
			catch (Exception ex)
			{
				ProcessOnLoadException(ex);
			}
			Point point = m_viewerControl.PointToScreen(Point.Empty);
			base.Left = point.X + Math.Max(0, (m_viewerControl.Width - base.Width) / 2);
			base.Top = point.Y + Math.Max(0, (m_viewerControl.Height - base.Height) / 2);
		}

		private void ProcessOnLoadException(Exception ex)
		{
			m_exportOperation?.Cleanup();
			m_exportOperation = null;
			m_viewerControl.DisplayErrorMsgBox(ex, LocalizationHelper.Current.ExportErrorTitle);
			Close();
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			if (m_cancelRequested)
			{
				return;
			}

			m_cancelRequested = true;
			cancelButton.Enabled = false;
			m_viewerControl.CancelRendering(0);
		}

		private void OnExportCompleteUI(object sender, AsyncCompletedEventArgs args)
		{
			var exportOperation = (AsyncExportOperation)sender;
			if (m_closing)
			{
				m_exportOperation = null;
				exportOperation.Cleanup();
				return;
			}

			try
			{
				NotifyExportProgress(exportOperation, args);
				if (args.Error != null)
				{
					if (!args.Cancelled)
					{
						m_viewerControl.DisplayErrorMsgBox(args.Error, LocalizationHelper.Current.ExportErrorTitle);
					}
				}
				else if (!string.IsNullOrWhiteSpace(exportOperation.OutputPath) && File.Exists(exportOperation.OutputPath))
				{
					string destinationPath = PromptFileName(exportOperation.FileNameExtension);
					if (!string.IsNullOrWhiteSpace(destinationPath))
					{
						if (File.Exists(destinationPath) && MessageBox.Show(this, ReportPreviewStrings.ReplaceExistingExportFile, ReportPreviewStrings.ExportMenuItemText.Replace("&", string.Empty), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
						{
							return;
						}

						CopyExportToDestination(exportOperation, destinationPath);
						if (openAfterExport.Checked)
						{
							OpenExport(destinationPath);
						}
						base.DialogResult = DialogResult.OK;
					}
				}
			}
			catch (Exception ex)
			{
				m_viewerControl.DisplayErrorMsgBox(ex, LocalizationHelper.Current.ExportErrorTitle);
			}
			finally
			{
				m_exportOperation = null;
				exportOperation.Cleanup();
				Close();
			}
		}

		private void OnExportComplete(object sender, AsyncCompletedEventArgs args)
		{
			if (m_closing || IsDisposed || Disposing || !IsHandleCreated)
			{
				((AsyncExportOperation)sender).Cleanup();
				return;
			}

			try
			{
				BeginInvoke(new MethodInvoker(() => OnExportCompleteUI(sender, args)));
			}
			catch (InvalidOperationException)
			{
				((AsyncExportOperation)sender).Cleanup();
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			m_closing = true;
			if (m_exportOperation != null)
			{
				m_viewerControl.CancelRendering(0);
			}
			base.OnFormClosing(e);
		}

		private string PromptFileName(string fileExtension)
		{
			_ = m_format.Name;
			using SaveFileDialog saveFileDialog = new SaveFileDialog();
			string str = "";
			if (fileExtension != null)
			{
				str = m_format.LocalizedName + " (*." + fileExtension + ")|*." + fileExtension + "|";
			}
			saveFileDialog.Filter = saveFileDialog.Filter + str + LocalizationHelper.Current.AllFilesFilter + " (*.*)|*.*";
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.OverwritePrompt = true;
			if (!string.IsNullOrWhiteSpace(s_lastExportDirectory) && Directory.Exists(s_lastExportDirectory))
			{
				saveFileDialog.InitialDirectory = s_lastExportDirectory;
			}
			bool flag = !string.IsNullOrEmpty(m_fileName);
			string text = m_fileName;
			if (!flag)
			{
				text = m_viewerControl.Report.DisplayNameForUse;
				text = ReplaceReservedCharacters(text);
				if (fileExtension != null)
				{
					text = text + "." + fileExtension;
				}
			}
			try
			{
				saveFileDialog.FileName = text;
			}
			catch (SecurityException)
			{
			}
			bool flag2 = flag;
			if (!flag && saveFileDialog.ShowDialog(this) == DialogResult.OK)
			{
				flag2 = true;
			}
			if (flag2)
			{
				s_lastExportDirectory = Path.GetDirectoryName(saveFileDialog.FileName);
				return saveFileDialog.FileName;
			}
			return null;
		}

		private static void OpenExport(string path)
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = path,
				UseShellExecute = true
			});
		}

		private static void CopyExportToDestination(AsyncExportOperation exportOperation, string destinationPath)
		{
			string fullDestinationPath = Path.GetFullPath(destinationPath);
			string destinationDirectory = Path.GetDirectoryName(fullDestinationPath);
			if (string.IsNullOrWhiteSpace(destinationDirectory))
			{
				throw new InvalidOperationException("The export destination directory is not valid.");
			}

			Directory.CreateDirectory(destinationDirectory);
			string stagingDirectory = Path.Combine(
				destinationDirectory,
				$".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.export");
			Directory.CreateDirectory(stagingDirectory);

			try
			{
				string stagedMainPath = Path.Combine(stagingDirectory, Path.GetFileName(fullDestinationPath));
				CopyFile(exportOperation.OutputPath, stagedMainPath);

                var stagedSecondaryFiles = new System.Collections.Generic.List<(string StagedPath, string DestinationPath)>();
                var relativePaths = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (ExportedStream secondaryStream in exportOperation.SecondaryStreams)
                {
                    if (secondaryStream.IsInternal)
                    {
                        continue;
                    }

                    string relativePath = GetSafeRelativePath(secondaryStream.Name, secondaryStream.Path);
					if (!relativePaths.Add(relativePath))
					{
						throw new InvalidOperationException($"The export contains duplicate secondary stream '{relativePath}'.");
					}

					string stagedPath = GetPathUnderDirectory(stagingDirectory, relativePath);
					string secondaryDestinationPath = GetPathUnderDirectory(destinationDirectory, relativePath);
					CopyFile(secondaryStream.Path, stagedPath);
					stagedSecondaryFiles.Add((stagedPath, secondaryDestinationPath));
				}

				foreach (var secondaryFile in stagedSecondaryFiles)
				{
					Directory.CreateDirectory(Path.GetDirectoryName(secondaryFile.DestinationPath));
					File.Move(secondaryFile.StagedPath, secondaryFile.DestinationPath, overwrite: true);
				}

				// Commit the main file last. It is fully prepared in the destination
				// directory, so the final replacement is atomic on the same volume.
				File.Move(stagedMainPath, fullDestinationPath, overwrite: true);
			}
			finally
			{
				try
				{
					if (Directory.Exists(stagingDirectory))
					{
						Directory.Delete(stagingDirectory, recursive: true);
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"ExportDialog.CleanupStagingDirectory: {ex.GetType().Name} - {ex.Message}");
				}
			}
		}

		private static void CopyFile(string sourcePath, string destinationPath)
		{
			string destinationDirectory = Path.GetDirectoryName(destinationPath);
			if (string.IsNullOrWhiteSpace(destinationDirectory))
			{
				throw new InvalidOperationException("The export destination directory is not valid.");
			}

			Directory.CreateDirectory(destinationDirectory);
			using (FileStream source = File.OpenRead(sourcePath))
			using (FileStream destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.SequentialScan))
			{
				source.CopyTo(destination);
				destination.Flush(flushToDisk: true);
			}
		}

		private void NotifyExportProgress(AsyncExportOperation operation, AsyncCompletedEventArgs args)
		{
			ReportRenderStage stage = args.Cancelled
				? ReportRenderStage.Cancelled
				: args.Error == null ? ReportRenderStage.Completed : ReportRenderStage.Failed;
			long? bytesRendered = null;
			if (args.Error == null && File.Exists(operation.OutputPath))
			{
				bytesRendered = new FileInfo(operation.OutputPath).Length;
			}

			m_viewerControl.NotifyRenderingProgress(new ReportRenderProgress(
				stage,
				m_format.Name,
				DateTime.UtcNow - operation.StartedAtUtc,
				bytesRendered,
				args.Error,
				args.Error?.Message ?? $"Report export {stage.ToString().ToLowerInvariant()}."));
		}

		private static string GetSafeRelativePath(string streamName, string fallbackPath)
		{
			string path = string.IsNullOrWhiteSpace(streamName) ? Path.GetFileName(fallbackPath) : streamName.Trim();
			path = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
			if (Path.IsPathRooted(path))
			{
				throw new InvalidOperationException($"The export contains an unsafe secondary stream path '{streamName}'.");
			}

			string[] segments = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			if (segments.Length == 0 || Array.Exists(segments, segment => segment == ".." || segment.IndexOf(Path.VolumeSeparatorChar) >= 0))
			{
				throw new InvalidOperationException($"The export contains an unsafe secondary stream path '{streamName}'.");
			}

			return Path.Combine(segments);
		}

		private static string GetPathUnderDirectory(string directory, string relativePath)
		{
			string root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			string fullPath = Path.GetFullPath(Path.Combine(directory, relativePath));
			if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException($"The export contains an unsafe secondary stream path '{relativePath}'.");
			}

			return fullPath;
		}

		private string ReplaceReservedCharacters(string original)
		{
			StringBuilder stringBuilder = new StringBuilder(original.Length);
			char[] array = original.ToCharArray();
			foreach (char c in array)
			{
				bool flag = false;
				char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
				foreach (char c2 in invalidFileNameChars)
				{
					if (c == c2)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}
	}
}
