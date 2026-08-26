using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Reporting.NETCore;

namespace Microsoft.ReportViewer.ReportWorker;

public static class Program
{
    public static int Main()
    {
        try
        {
            var line = Console.In.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) throw new InvalidDataException("No worker request was supplied.");
            var request = ReportWorkerProtocol.Deserialize(line);
            Console.Out.WriteLine(JsonSerializer.Serialize(new ReportWorkerEvent("started", "Report worker started.")));
            Console.Out.Flush();
            var result = HeadlessReportRenderer.Render(request.Report);
            var streams = result.Streams.Select(stream => new ReportWorkerStream(stream.Name, stream.Extension, stream.MimeType, stream.Encoding, stream.Content)).ToArray();
            Console.Out.WriteLine(JsonSerializer.Serialize(new ReportWorkerEvent("output", "Report output is ready.", result.Content.LongLength)));
            Console.Out.Flush();
            Console.Out.WriteLine(ReportWorkerProtocol.SerializeResult(new ReportWorkerResult(result.Format, result.MimeType, result.Extension, result.Content, streams, Array.Empty<ReportWorkerDiagnostic>())));
            Console.Out.Flush();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"{exception.GetType().Name}: {exception.Message}");
            return 1;
        }
    }
}
