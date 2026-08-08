using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Reporting.WinForms
{

/// <summary>
/// Complete local report state returned when live reload detects a changed source.
/// </summary>
public sealed class ReportViewerReloadSnapshot
{
    public ReportViewerReloadSnapshot(
        byte[] reportDefinition,
        IReadOnlyList<ReportDataSource> dataSources,
        IReadOnlyList<ReportParameter> parameters)
    {
        ReportDefinition = reportDefinition;
        DataSources = dataSources;
        Parameters = parameters;
    }

    public ReportViewerReloadSnapshot(
        byte[] reportDefinition,
        IReadOnlyList<ReportDataSource> dataSources)
        : this(reportDefinition, dataSources, Array.Empty<ReportParameter>())
    {
    }

    public byte[] ReportDefinition { get; }

    public IReadOnlyList<ReportDataSource> DataSources { get; }

    public IReadOnlyList<ReportParameter> Parameters { get; }
}

/// <summary>
/// Configures checksum-based live reload for a local ReportViewer.
/// </summary>
public sealed class ReportViewerLiveReloadOptions
{
    private bool m_enabled;
    private string m_reportDefinitionPath;
    private string[] m_dataFilePaths = Array.Empty<string>();
    private TimeSpan m_pollInterval = TimeSpan.FromSeconds(2);
    private TimeSpan m_debounceDelay = TimeSpan.FromMilliseconds(250);
    private TimeSpan m_stabilityDelay = TimeSpan.FromMilliseconds(100);
    private int m_maxStableReadAttempts = 3;
    private Func<CancellationToken, ValueTask<ReportViewerReloadSnapshot>> m_reloadAsync;

    [DefaultValue(false)]
    public bool Enabled
    {
        get => m_enabled;
        set
        {
            if (m_enabled == value)
            {
                return;
            }

            m_enabled = value;
            OnChanged();
        }
    }

    public string ReportDefinitionPath
    {
        get => m_reportDefinitionPath;
        set
        {
            if (string.Equals(m_reportDefinitionPath, value, StringComparison.Ordinal))
            {
                return;
            }

            m_reportDefinitionPath = value;
            OnChanged();
        }
    }

    public string[] DataFilePaths
    {
        get => m_dataFilePaths;
        set
        {
            m_dataFilePaths = value ?? Array.Empty<string>();
            OnChanged();
        }
    }

    public TimeSpan PollInterval
    {
        get => m_pollInterval;
        set
        {
            if (m_pollInterval == value)
            {
                return;
            }

            m_pollInterval = value;
            OnChanged();
        }
    }

    public TimeSpan DebounceDelay
    {
        get => m_debounceDelay;
        set
        {
            if (m_debounceDelay == value)
            {
                return;
            }

            m_debounceDelay = value;
            OnChanged();
        }
    }

    public TimeSpan StabilityDelay
    {
        get => m_stabilityDelay;
        set
        {
            if (m_stabilityDelay == value)
            {
                return;
            }

            m_stabilityDelay = value;
            OnChanged();
        }
    }

    public int MaxStableReadAttempts
    {
        get => m_maxStableReadAttempts;
        set
        {
            if (m_maxStableReadAttempts == value)
            {
                return;
            }

            m_maxStableReadAttempts = value;
            OnChanged();
        }
    }

    public Func<CancellationToken, ValueTask<ReportViewerReloadSnapshot>> ReloadAsync
    {
        get => m_reloadAsync;
        set
        {
            if (ReferenceEquals(m_reloadAsync, value))
            {
                return;
            }

            m_reloadAsync = value;
            OnChanged();
        }
    }

    internal event EventHandler Changed;

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}

public sealed class ReportViewerLiveReloadErrorEventArgs : EventArgs
{
    public ReportViewerLiveReloadErrorEventArgs(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    public Exception Exception { get; }
}

internal static class ReportFileFingerprint
{
    public static async ValueTask<string> ComputeAsync(
        string reportDefinitionPath,
        IEnumerable<string> dataFilePaths,
        CancellationToken cancellationToken,
        int maxAttempts)
    {
        var reportFingerprint = string.IsNullOrWhiteSpace(reportDefinitionPath)
            ? string.Empty
            : await ComputeFileAsync(reportDefinitionPath, cancellationToken, maxAttempts).ConfigureAwait(true);
        var dataFingerprint = await ComputeFilesAsync(dataFilePaths, cancellationToken, maxAttempts).ConfigureAwait(true);
        return Combine($"template:{reportFingerprint}", $"data:{dataFingerprint}");
    }

    private static async ValueTask<string> ComputeFilesAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken,
        int maxAttempts)
    {
        var normalizedPaths = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var entries = new List<string>(normalizedPaths.Length);
        foreach (var path in normalizedPaths)
        {
            var fingerprint = await ComputeFileAsync(path, cancellationToken, maxAttempts).ConfigureAwait(true);
            entries.Add($"{path}\u001f{fingerprint}");
        }

        return Combine(entries.ToArray());
    }

    private static async ValueTask<string> ComputeFileAsync(
        string path,
        CancellationToken cancellationToken,
        int maxAttempts)
    {
        var fullPath = Path.GetFullPath(path);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var before = GetState(fullPath);
            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                64 * 1024,
                useAsync: true);
            using var algorithm = SHA256.Create();
            var hash = await algorithm.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(true);
            var after = GetState(fullPath);
            if (before.Equals(after))
            {
                return Convert.ToHexString(hash);
            }

            if (attempt + 1 < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50 * (attempt + 1)), cancellationToken)
                    .ConfigureAwait(true);
            }
        }

        throw new IOException($"File '{fullPath}' changed while its fingerprint was being calculated.");
    }

    private static string Combine(params string[] values) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\u001e', values))));

    private static FileState GetState(string path)
    {
        var info = new FileInfo(path);
        info.Refresh();
        if (!info.Exists)
        {
            throw new FileNotFoundException("The fingerprint source file was not found.", path);
        }

        return new FileState(info.Length, info.LastWriteTimeUtc, info.CreationTimeUtc);
    }

    private sealed class FileState : IEquatable<FileState>
    {
        public FileState(long length, DateTime lastWriteTimeUtc, DateTime creationTimeUtc)
        {
            Length = length;
            LastWriteTimeUtc = lastWriteTimeUtc;
            CreationTimeUtc = creationTimeUtc;
        }

        public long Length { get; }

        public DateTime LastWriteTimeUtc { get; }

        public DateTime CreationTimeUtc { get; }

        public bool Equals(FileState other) =>
            other != null &&
            Length == other.Length &&
            LastWriteTimeUtc == other.LastWriteTimeUtc &&
            CreationTimeUtc == other.CreationTimeUtc;

        public override bool Equals(object obj) => Equals(obj as FileState);

        public override int GetHashCode() =>
            Tuple.Create(Length, LastWriteTimeUtc, CreationTimeUtc).GetHashCode();
    }
}
}
