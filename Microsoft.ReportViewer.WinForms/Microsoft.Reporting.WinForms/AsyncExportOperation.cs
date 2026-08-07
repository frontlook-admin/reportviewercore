using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.ReportingServices.Interfaces;

namespace Microsoft.Reporting.WinForms
{
    internal sealed class ExportedStream
    {
        public ExportedStream(string name, string path, bool isInternal)
        {
            Name = name;
            Path = path;
            IsInternal = isInternal;
        }

        public string Name { get; }

        public string Path { get; }

        public bool IsInternal { get; }
    }

    internal sealed class AsyncExportOperation : AsyncRenderingOperation
    {
        private readonly string m_outputPath;
        private readonly object m_streamLock = new object();
        private readonly List<Stream> m_openStreams = new List<Stream>();
        private readonly List<string> m_temporaryPaths = new List<string>();
        private readonly List<ExportedStream> m_secondaryStreams = new List<ExportedStream>();

        private int m_mainStreamCreated;
        private int m_cleanedUp;
        private string m_fileNameExtension;

        public string OutputPath => m_outputPath;

        public string FileNameExtension => m_fileNameExtension;

        public IReadOnlyList<ExportedStream> SecondaryStreams
        {
            get
            {
                lock (m_streamLock)
                {
                    return m_secondaryStreams.ToArray();
                }
            }
        }

        public AsyncExportOperation(Report report, PageCountMode pageCountMode, string format, string deviceInfo, bool allowInternalRenderers, PostRenderArgs postRenderArgs)
            : base(report, pageCountMode, format, deviceInfo, allowInternalRenderers, postRenderArgs)
        {
            var directory = Path.Combine(Path.GetTempPath(), "RdlcReportViewer", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            m_outputPath = Path.Combine(directory, "report-output");
            m_temporaryPaths.Add(m_outputPath);
        }

        protected override void RenderServerReport(ServerReport report)
        {
            using (var stream = OpenTrackedFileStream(m_outputPath))
            {
                report.InternalRender(
                    isAbortable: true,
                    base.Format,
                    base.DeviceInfo,
                    GetBaseServerUrlParameters(),
                    stream,
                    out _,
                    out m_fileNameExtension);
            }

            EnsureOutputExists();
        }

        protected override void RenderLocalReport(LocalReport report)
        {
            try
            {
                report.InternalRender(
                    base.Format,
                    base.AllowInternalRenderers,
                    base.DeviceInfo,
                    base.PageCountMode,
                    CreateExportStream,
                    out m_warnings);
            }
            finally
            {
                CloseOpenStreams();
            }

            EnsureOutputExists();
        }

        public void Cleanup()
        {
            if (Interlocked.Exchange(ref m_cleanedUp, 1) != 0)
            {
                return;
            }

            CloseOpenStreams();

            string[] paths;
            lock (m_streamLock)
            {
                paths = m_temporaryPaths.ToArray();
                m_temporaryPaths.Clear();
            }

            foreach (var path in paths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AsyncExportOperation.Cleanup: {ex.GetType().Name} - {ex.Message}");
                }
            }

            try
            {
                var directory = Path.GetDirectoryName(m_outputPath);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AsyncExportOperation.CleanupDirectory: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private Stream CreateExportStream(string name, string extension, Encoding encoding, string mimeType, bool willSeek, StreamOper operation)
        {
            if (operation == StreamOper.RegisterOnly)
            {
                return null;
            }

            if (operation == StreamOper.CreateAndRegister && Interlocked.CompareExchange(ref m_mainStreamCreated, 1, 0) == 0)
            {
                m_fileNameExtension = extension;
                return OpenTrackedFileStream(m_outputPath);
            }

            var secondaryPath = Path.Combine(
                Path.GetDirectoryName(m_outputPath),
                $"stream-{Guid.NewGuid():N}{(string.IsNullOrEmpty(extension) ? ".tmp" : "." + extension)}");
            lock (m_streamLock)
            {
                m_temporaryPaths.Add(secondaryPath);
                m_secondaryStreams.Add(new ExportedStream(
                    name,
                    secondaryPath,
                    string.Equals(name, "NonSharedCache", StringComparison.OrdinalIgnoreCase)));
            }
            return OpenTrackedFileStream(secondaryPath);
        }

        private void EnsureOutputExists()
        {
            if (!File.Exists(m_outputPath) || new FileInfo(m_outputPath).Length == 0)
            {
                throw new InvalidOperationException("The report export did not produce an output stream.");
            }
        }

        private FileStream OpenTrackedFileStream(string path)
        {
            var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 64 * 1024,
                options: FileOptions.SequentialScan);
            lock (m_streamLock)
            {
                m_openStreams.Add(stream);
            }
            return stream;
        }

        private void CloseOpenStreams()
        {
            Stream[] streams;
            lock (m_streamLock)
            {
                streams = m_openStreams.ToArray();
                m_openStreams.Clear();
            }

            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }
    }
}
