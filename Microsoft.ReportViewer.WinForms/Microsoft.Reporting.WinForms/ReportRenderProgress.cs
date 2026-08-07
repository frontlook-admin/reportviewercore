using System;

namespace Microsoft.Reporting.WinForms
{
    public enum ReportRenderStage
    {
        Started,
        Rendering,
        Completed,
        Cancelled,
        Failed
    }

    public sealed class ReportRenderProgress : EventArgs
    {
        public ReportRenderProgress(ReportRenderStage stage, string format, TimeSpan elapsed, long? bytesRendered, Exception error, string message)
        {
            Stage = stage;
            Format = format;
            Elapsed = elapsed;
            BytesRendered = bytesRendered;
            Error = error;
            Message = message;
        }

        public ReportRenderStage Stage { get; }

        public string Format { get; }

        public TimeSpan Elapsed { get; }

        public long? BytesRendered { get; }

        public Exception Error { get; }

        public string Message { get; }
    }
}
