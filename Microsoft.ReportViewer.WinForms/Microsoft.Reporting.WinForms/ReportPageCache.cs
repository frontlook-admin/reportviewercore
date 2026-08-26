using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Reporting.WinForms
{

/// <summary>Bounded, thread-safe LRU cache for rendered report pages.</summary>
public sealed class ReportPageCache<TPage> : IDisposable
{
    private sealed class Entry
    {
        public Entry(TPage value, LinkedListNode<int> node)
        {
            Value = value;
            Node = node;
        }

        public TPage Value { get; }
        public LinkedListNode<int> Node { get; }
    }

    private readonly object _gate = new object();
    private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
    private readonly Dictionary<int, Task<TPage>> _inFlight = new Dictionary<int, Task<TPage>>();
    private readonly LinkedList<int> _recent = new LinkedList<int>();
    private readonly Action<TPage> _dispose;
    private bool _disposed;
    private int _capacity;

    public ReportPageCache(int capacity = 8, Action<TPage> dispose = null)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
        _dispose = dispose ?? (value =>
        {
            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        });
    }

    public int Capacity
    {
        get { lock (_gate) return _capacity; }
        set
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
            List<TPage> evicted;
            lock (_gate)
            {
                ThrowIfDisposed();
                _capacity = value;
                evicted = EvictIfNeeded();
            }
            DisposeValues(evicted);
        }
    }

    public int Count
    {
        get { lock (_gate) return _entries.Count; }
    }

    public IReadOnlyList<int> CachedPages
    {
        get { lock (_gate) return _recent.ToArray(); }
    }

    public event EventHandler<int> PageEvicted;

    public async Task<TPage> GetOrAddAsync(
        int page,
        Func<CancellationToken, Task<TPage>> factory,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        Task<TPage> pending;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_entries.TryGetValue(page, out Entry entry))
            {
                Touch(entry);
                return entry.Value;
            }

            if (!_inFlight.TryGetValue(page, out pending))
            {
                pending = RenderPageAsync(page, factory);
                _inFlight.Add(page, pending);
            }
        }

        return await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PrefetchAsync(
        IEnumerable<int> pages,
        int firstPage,
        int lastPage,
        Func<int, Task<TPage>> factory,
        CancellationToken cancellationToken = default)
    {
        if (pages == null) throw new ArgumentNullException(nameof(pages));
        if (factory == null) throw new ArgumentNullException(nameof(factory));
        if (firstPage < 1 || lastPage < firstPage) throw new ArgumentOutOfRangeException(nameof(lastPage));

        var work = new List<Task<TPage>>();
        var seen = new HashSet<int>();
        foreach (int page in pages)
        {
            if (page < firstPage || page > lastPage || !seen.Add(page)) continue;
            work.Add(GetOrAddAsync(page, _ => factory(page), cancellationToken));
        }
        await Task.WhenAll(work).ConfigureAwait(false);
    }

    public bool Remove(int page)
    {
        TPage value;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_entries.TryGetValue(page, out Entry entry)) return false;
            value = entry.Value;
            RemoveEntry(entry);
        }
        DisposeValue(value);
        PageEvicted?.Invoke(this, page);
        return true;
    }

    public void Clear()
    {
        List<TPage> values;
        lock (_gate)
        {
            ThrowIfDisposed();
            values = _entries.Values.Select(entry => entry.Value).ToList();
            _entries.Clear();
            _recent.Clear();
        }
        DisposeValues(values);
    }

    public void Dispose()
    {
        List<TPage> values;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            values = _entries.Values.Select(entry => entry.Value).ToList();
            _entries.Clear();
            _recent.Clear();
        }
        DisposeValues(values);
        GC.SuppressFinalize(this);
    }

    private async Task<TPage> RenderPageAsync(int page, Func<CancellationToken, Task<TPage>> factory)
    {
        try
        {
            TPage value = await factory(CancellationToken.None).ConfigureAwait(false);
            List<TPage> evicted;
            lock (_gate)
            {
                _inFlight.Remove(page);
                if (_disposed)
                {
                    DisposeValue(value);
                    throw new ObjectDisposedException(nameof(ReportPageCache<TPage>));
                }
                if (_entries.ContainsKey(page))
                {
                    DisposeValue(value);
                    return _entries[page].Value;
                }
                LinkedListNode<int> node = _recent.AddFirst(page);
                _entries.Add(page, new Entry(value, node));
                evicted = EvictIfNeeded();
            }
            DisposeValues(evicted);
            return value;
        }
        catch
        {
            lock (_gate)
            {
                _inFlight.Remove(page);
            }
            throw;
        }
    }

    private List<TPage> EvictIfNeeded()
    {
        var evicted = new List<TPage>();
        while (_entries.Count > _capacity)
        {
            LinkedListNode<int> node = _recent.Last;
            Entry entry = _entries[node.Value];
            RemoveEntry(entry);
            evicted.Add(entry.Value);
            PageEvicted?.Invoke(this, node.Value);
        }
        return evicted;
    }

    private void Touch(Entry entry)
    {
        _recent.Remove(entry.Node);
        _recent.AddFirst(entry.Node);
    }

    private void RemoveEntry(Entry entry)
    {
        _entries.Remove(entry.Node.Value);
        _recent.Remove(entry.Node);
    }

    private void DisposeValues(IEnumerable<TPage> values)
    {
        foreach (TPage value in values) DisposeValue(value);
    }

    private void DisposeValue(TPage value)
    {
        try { _dispose(value); } catch (ObjectDisposedException) { }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ReportPageCache<TPage>));
    }
}
}
