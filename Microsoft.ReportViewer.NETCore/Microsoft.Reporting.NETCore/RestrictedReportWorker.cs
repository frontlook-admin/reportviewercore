#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Reporting.WinForms;

namespace Microsoft.Reporting.NETCore;

public sealed record ReportWorkerRequest(HeadlessReportRequest Report);
public sealed record ReportWorkerStream(string Name, string Extension, string MimeType, string Encoding, byte[] Content);
public sealed record ReportWorkerDiagnostic(string Code, string Message);
public sealed record ReportWorkerResult(string Format, string MimeType, string Extension, byte[] Content, IReadOnlyList<ReportWorkerStream> Streams, IReadOnlyList<ReportWorkerDiagnostic> Diagnostics);
public sealed record ReportWorkerEvent(string Type, string? Message = null, long? Bytes = null);

public sealed class ReportWorkerClientOptions
{
    public ReportWorkerClientOptions(string workerPath) => WorkerPath = string.IsNullOrWhiteSpace(workerPath) ? throw new ArgumentException("Worker path is required.", nameof(workerPath)) : workerPath;
    public string WorkerPath { get; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    public long MaxOutputBytes { get; set; } = 64 * 1024 * 1024;
    public long MemoryLimitBytes { get; set; } = 512 * 1024 * 1024;
    public string? WorkingDirectory { get; set; }
    public bool UseRestrictedWindowsToken { get; set; }
}

public interface IReportWorkerTransport
{
    Task<ReportWorkerResult> ExecuteAsync(ReportWorkerRequest request, IProgress<ReportWorkerEvent>? progress, CancellationToken cancellationToken);
}

public sealed class ReportWorkerSecurityException : InvalidOperationException
{
    public ReportWorkerSecurityException(string message) : base(message) { }
}
public sealed class ReportWorkerTimeoutException : TimeoutException
{
    public ReportWorkerTimeoutException(TimeSpan timeout) : base($"The report worker exceeded its {timeout} timeout.") => Timeout = timeout;
    public TimeSpan Timeout { get; }
}
public sealed class ReportWorkerProcessException : InvalidOperationException
{
    public ReportWorkerProcessException(string message) : base(message) { }
}

public sealed class RestrictedReportWorkerClient
{
    private readonly ReportWorkerClientOptions options;
    private readonly IReportWorkerTransport transport;

    public RestrictedReportWorkerClient(ReportWorkerClientOptions options, IReportWorkerTransport? transport = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.transport = transport ?? new ProcessReportWorkerTransport(options);
    }

    public ReportWorkerResult Render(ReportWorkerRequest request, IProgress<ReportWorkerEvent>? progress = null)
        => RenderAsync(request, progress).GetAwaiter().GetResult();

    public async Task<ReportWorkerResult> RenderAsync(ReportWorkerRequest request, IProgress<ReportWorkerEvent>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateBeforeLaunch(request);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        try
        {
            return await transport.ExecuteAsync(request, progress, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new ReportWorkerTimeoutException(options.Timeout);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ReportWorkerSecurityException) { throw; }
        catch (Exception exception) when (exception is not ReportWorkerProcessException)
        {
            throw new ReportWorkerProcessException($"The report worker failed: {exception.Message}");
        }
    }

    private static void ValidateBeforeLaunch(ReportWorkerRequest request)
    {
        var policy = request.Report.SecurityPolicy ?? ReportSecurityPolicy.Restrictive;
        var definition = Encoding.UTF8.GetString(request.Report.Definition);
        var analysis = policy.Analyze(definition, request.Report.IsTrusted);
        var errors = analysis.Diagnostics.Where(item => item.Severity == ReportSecurityDiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
            throw new ReportWorkerSecurityException(string.Join(" ", errors.Select(item => item.Message)));
    }
}

public static class ReportWorkerProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static string Serialize(ReportWorkerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return JsonSerializer.Serialize(ToWire(request), JsonOptions);
    }

    public static ReportWorkerRequest Deserialize(string json)
    {
        var wire = JsonSerializer.Deserialize<RequestWire>(json, JsonOptions) ?? throw new InvalidDataException("Worker request is empty.");
        if (string.IsNullOrWhiteSpace(wire.Definition)) throw new InvalidDataException("Worker request has no report definition.");
        var sources = (wire.DataSources ?? Array.Empty<DataSourceWire>()).Select(source => new HeadlessReportDataSource(source.Name, DeserializeTable(source.Data))).ToArray();
        var parameters = (wire.Parameters ?? new Dictionary<string, string[]>()).ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value, StringComparer.OrdinalIgnoreCase);
        var subreports = (wire.Subreports ?? Array.Empty<SubreportWire>()).Select(subreport => new HeadlessSubreportDefinition(subreport.Name,
            Convert.FromBase64String(subreport.Definition), (subreport.DataSources ?? Array.Empty<DataSourceWire>()).Select(source =>
                new HeadlessReportDataSource(source.Name, DeserializeTable(source.Data))))).ToArray();
        return new ReportWorkerRequest(new HeadlessReportRequest(Convert.FromBase64String(wire.Definition), sources, parameters,
            subreports, format: wire.Format ?? "PDF", deviceInfo: wire.DeviceInfo, securityPolicy: wire.Restrictive ? ReportSecurityPolicy.Restrictive : null, isTrusted: wire.IsTrusted));
    }

    public static string SerializeResult(ReportWorkerResult result) => JsonSerializer.Serialize(result, JsonOptions);
    public static ReportWorkerResult DeserializeResult(string json) => JsonSerializer.Deserialize<ReportWorkerResult>(json, JsonOptions) ?? throw new InvalidDataException("Worker result is empty.");

    private static RequestWire ToWire(ReportWorkerRequest request) => new()
    {
        Definition = Convert.ToBase64String(request.Report.Definition), Format = request.Report.Format, DeviceInfo = request.Report.DeviceInfo,
        IsTrusted = request.Report.IsTrusted, Restrictive = true,
        DataSources = request.Report.DataSources.Select(source => new DataSourceWire { Name = source.Name, Data = SerializeTable(source.Data) }).ToArray(),
        Subreports = request.Report.Subreports.Select(subreport => new SubreportWire { Name = subreport.Name, Definition = Convert.ToBase64String(subreport.Definition),
            DataSources = subreport.DataSources.Select(source => new DataSourceWire { Name = source.Name, Data = SerializeTable(source.Data) }).ToArray() }).ToArray(),
        Parameters = request.Report.Parameters.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase)
    };

    private static string SerializeTable(DataTable table)
    {
        using var data = new DataSet("ReportData"); data.Tables.Add(table.Copy());
        using var stream = new MemoryStream(); data.WriteXml(stream, XmlWriteMode.WriteSchema); return Convert.ToBase64String(stream.ToArray());
    }
    private static DataTable DeserializeTable(string encoded)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(encoded)); var data = new DataSet(); data.ReadXml(stream, XmlReadMode.ReadSchema);
        return data.Tables.Count == 0 ? throw new InvalidDataException("Worker data source has no table.") : data.Tables[0];
    }
    private sealed class RequestWire
    {
        public string? Definition { get; set; } public string? Format { get; set; } public string? DeviceInfo { get; set; } public bool IsTrusted { get; set; } public bool Restrictive { get; set; }
        public DataSourceWire[]? DataSources { get; set; } public SubreportWire[]? Subreports { get; set; } public Dictionary<string, string[]>? Parameters { get; set; }
    }
    private sealed class DataSourceWire { public string Name { get; set; } = ""; public string Data { get; set; } = ""; }
    private sealed class SubreportWire { public string Name { get; set; } = ""; public string Definition { get; set; } = ""; public DataSourceWire[]? DataSources { get; set; } }
}

internal sealed class ProcessReportWorkerTransport : IReportWorkerTransport
{
    private readonly ReportWorkerClientOptions options;
    public ProcessReportWorkerTransport(ReportWorkerClientOptions options) => this.options = options;

    public async Task<ReportWorkerResult> ExecuteAsync(ReportWorkerRequest request, IProgress<ReportWorkerEvent>? progress, CancellationToken cancellationToken)
    {
        var start = WorkerCommand.Create(options);
        var temporaryDirectory = options.WorkingDirectory == null ? Directory.CreateTempSubdirectory("report-worker-") : null;
        if (temporaryDirectory != null) start.WorkingDirectory = temporaryDirectory.FullName;
        WindowsRestrictedProcess? restrictedProcess = null;
        Process? process = null;
        try
        {
            restrictedProcess = options.UseRestrictedWindowsToken && OperatingSystem.IsWindows() ? WindowsRestrictedProcess.Start(start) : null;
            process = restrictedProcess?.Process ?? new Process { StartInfo = start };
            if (restrictedProcess == null && !process.Start()) throw new ReportWorkerProcessException("The report worker process could not be started.");
            var standardInput = restrictedProcess?.StandardInput ?? process.StandardInput;
            var standardOutput = restrictedProcess?.StandardOutput ?? process.StandardOutput;
            var standardError = restrictedProcess?.StandardError ?? process.StandardError;
            using var job = WindowsJobResourceLimit.Attach(process, options.MemoryLimitBytes);
            try
            {
                await standardInput.WriteLineAsync(ReportWorkerProtocol.Serialize(request)).ConfigureAwait(false);
            }
            catch (IOException) when (process.HasExited)
            {
                throw new ReportWorkerProcessException($"The restricted report worker exited with code {process.ExitCode}: {await standardError.ReadToEndAsync().ConfigureAwait(false)}");
            }
            standardInput.Close();
            string? resultLine = null; long outputBytes = 0;
            while (true)
            {
                var line = await standardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null) break;
                outputBytes += Encoding.UTF8.GetByteCount(line);
                if (outputBytes > options.MaxOutputBytes) throw new ReportWorkerProcessException("The report worker exceeded the output limit.");
                if (line.StartsWith("{\"type\":", StringComparison.Ordinal)) { progress?.Report(JsonSerializer.Deserialize<ReportWorkerEvent>(line)!); continue; }
                resultLine = line;
            }
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            if (process.ExitCode != 0) throw new ReportWorkerProcessException($"The report worker exited with code {process.ExitCode}: {await standardError.ReadToEndAsync().ConfigureAwait(false)}");
            if (resultLine == null)
            {
                var error = await standardError.ReadToEndAsync().ConfigureAwait(false);
                throw new ReportWorkerProcessException($"The report worker returned no result (exit code {process.ExitCode}): {error}");
            }
            return ReportWorkerProtocol.DeserializeResult(resultLine);
        }
        catch
        {
            if (process != null && !process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
        finally { restrictedProcess?.Dispose(); process?.Dispose(); temporaryDirectory?.Delete(recursive: true); }
    }
}

internal static class WorkerCommand
{
    public static ProcessStartInfo Create(ReportWorkerClientOptions options)
    {
        var workerPath = Path.GetFullPath(options.WorkerPath);
        if (!Path.IsPathFullyQualified(workerPath) || !File.Exists(workerPath))
            throw new ReportWorkerSecurityException($"The report worker path is missing or is not absolute: {options.WorkerPath}");
        var isDll = string.Equals(Path.GetExtension(options.WorkerPath), ".dll", StringComparison.OrdinalIgnoreCase);
        var executable = isDll ? ResolveDotnetPath() : workerPath;
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = options.WorkingDirectory == null ? Path.GetDirectoryName(workerPath)! : Path.GetFullPath(options.WorkingDirectory), UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        if (!Directory.Exists(info.WorkingDirectory)) throw new ReportWorkerSecurityException($"The report worker working directory does not exist: {info.WorkingDirectory}");
        info.Environment.Clear();
        foreach (var name in new[] { "SystemRoot", "PATH", "TEMP", "TMP", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "DOTNET_ROOT", "DOTNET_ROOT_X64" })
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value)) info.Environment[name] = value;
        }
        if (isDll) info.ArgumentList.Add(workerPath);
        return info;
    }

    private static string ResolveDotnetPath()
    {
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(host) && Path.IsPathFullyQualified(host) && File.Exists(host)) return host;
        var candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        if (OperatingSystem.IsWindows() && File.Exists(candidate)) return candidate;
        return OperatingSystem.IsWindows() ? throw new ReportWorkerSecurityException("The dotnet host could not be resolved to an absolute path.") : "dotnet";
    }
}

internal sealed class WindowsJobResourceLimit : IDisposable
{
    private IntPtr handle;
    private WindowsJobResourceLimit(IntPtr handle) => this.handle = handle;
    public static WindowsJobResourceLimit Attach(Process process, long memoryBytes)
    {
        if (!OperatingSystem.IsWindows()) return new WindowsJobResourceLimit(IntPtr.Zero);
        var job = CreateJobObject(IntPtr.Zero, null); if (job == IntPtr.Zero) return new WindowsJobResourceLimit(IntPtr.Zero);
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = 0x100 }, ProcessMemoryLimit = (UIntPtr)memoryBytes };
        if (!SetInformationJobObject(job, 9, ref info, (uint)Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()) || !AssignProcessToJobObject(job, process.Handle)) CloseHandle(job);
        return new WindowsJobResourceLimit(job);
    }
    public void Dispose() { if (handle != IntPtr.Zero) { CloseHandle(handle); handle = IntPtr.Zero; } }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateJobObject(IntPtr attributes, string? name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(IntPtr job, int type, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, uint length);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
    [StructLayout(LayoutKind.Sequential)] private struct JOBOBJECT_BASIC_LIMIT_INFORMATION { public long PerProcessUserTimeLimit; public long PerJobUserTimeLimit; public uint LimitFlags; public UIntPtr MinimumWorkingSetSize; public UIntPtr MaximumWorkingSetSize; public uint ActiveProcessLimit; public IntPtr Affinity; public uint PriorityClass; public uint SchedulingClass; }
    [StructLayout(LayoutKind.Sequential)] private struct IO_COUNTERS { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
    [StructLayout(LayoutKind.Sequential)] private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION { public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation; public IO_COUNTERS IoInfo; public UIntPtr ProcessMemoryLimit, JobMemoryLimit; public UIntPtr PeakProcessMemoryUsed, PeakJobMemoryUsed; }
}
