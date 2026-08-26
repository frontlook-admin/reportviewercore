using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace Microsoft.Reporting.WinForms
{

public enum ReportNavigationKind
{
    Page,
    Bookmark,
    DocumentMap,
    Drillthrough,
    Search
}

public sealed class ReportNavigationEntry : IEquatable<ReportNavigationEntry>
{
    public ReportNavigationEntry(string reportKey, int pageNumber, ReportNavigationKind kind, string target)
    {
        if (string.IsNullOrWhiteSpace(reportKey)) throw new ArgumentException("A report key is required.", nameof(reportKey));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        ReportKey = reportKey;
        PageNumber = pageNumber;
        Kind = kind;
        Target = target;
    }

    public string ReportKey { get; }
    public int PageNumber { get; }
    public ReportNavigationKind Kind { get; }
    public string Target { get; }

    public bool Equals(ReportNavigationEntry other) => other != null
        && string.Equals(ReportKey, other.ReportKey, StringComparison.Ordinal)
        && PageNumber == other.PageNumber && Kind == other.Kind
        && string.Equals(Target, other.Target, StringComparison.Ordinal);

    public override bool Equals(object obj) => Equals(obj as ReportNavigationEntry);
    public override int GetHashCode() => HashCode.Combine(ReportKey, PageNumber, Kind, Target);
}

/// <summary>Bounded, disposable back/forward state for preview navigation.</summary>
public sealed class ReportNavigationHistory : IDisposable
{
    private readonly List<ReportNavigationEntry> m_entries = new List<ReportNavigationEntry>();
    private int m_position = -1;
    private bool m_disposed;

    public ReportNavigationHistory(int capacity = 100)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    public int Capacity { get; }
    public bool CanGoBack => !m_disposed && m_position > 0;
    public bool CanGoForward => !m_disposed && m_position >= 0 && m_position < m_entries.Count - 1;
    public ReportNavigationEntry Current => !m_disposed && m_position >= 0 ? m_entries[m_position] : null;
    public IReadOnlyList<ReportNavigationEntry> Entries => new ReadOnlyCollection<ReportNavigationEntry>(m_entries.ToArray());

    public void Record(ReportNavigationEntry entry)
    {
        EnsureNotDisposed();
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        if (Current != null && Current.Equals(entry)) return;
        if (m_position < m_entries.Count - 1)
        {
            m_entries.RemoveRange(m_position + 1, m_entries.Count - m_position - 1);
        }
        m_entries.Add(entry);
        if (m_entries.Count > Capacity) m_entries.RemoveAt(0);
        m_position = m_entries.Count - 1;
    }

    public bool TryGoBack(out ReportNavigationEntry entry) => TryMove(-1, out entry);
    public bool TryGoForward(out ReportNavigationEntry entry) => TryMove(1, out entry);

    private bool TryMove(int offset, out ReportNavigationEntry entry)
    {
        entry = null;
        if (!CanGoBack && offset < 0 || !CanGoForward && offset > 0) return false;
        m_position += offset;
        entry = m_entries[m_position];
        return true;
    }

    public void Clear()
    {
        if (m_disposed) return;
        m_entries.Clear();
        m_position = -1;
    }

    public void Dispose()
    {
        if (m_disposed) return;
        Clear();
        m_disposed = true;
    }

    private void EnsureNotDisposed()
    {
        if (m_disposed) throw new ObjectDisposedException(nameof(ReportNavigationHistory));
    }
}
}
