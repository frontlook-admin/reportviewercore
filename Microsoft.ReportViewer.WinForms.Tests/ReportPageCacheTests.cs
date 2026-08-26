using System.Collections.Concurrent;
using Microsoft.Reporting.WinForms;
using Xunit;

namespace Microsoft.ReportViewer.WinForms.Tests;

public sealed class ReportPageCacheTests
{
    [Fact]
    public async Task Cache_evicts_least_recently_used_page_and_disposes_values()
    {
        var disposed = new ConcurrentBag<int>();
        using var cache = new ReportPageCache<int>(2, value => disposed.Add(value));

        await cache.GetOrAddAsync(1, _ => Task.FromResult(1));
        await cache.GetOrAddAsync(2, _ => Task.FromResult(2));
        _ = await cache.GetOrAddAsync(1, _ => Task.FromResult(10));
        await cache.GetOrAddAsync(3, _ => Task.FromResult(3));

        cache.CachedPages.Should().BeEquivalentTo(new[] { 1, 3 });
        disposed.Should().Contain(2);
    }

    [Fact]
    public async Task Cache_deduplicates_concurrent_page_factories()
    {
        using var cache = new ReportPageCache<string>(3);
        var calls = 0;
        var gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = cache.GetOrAddAsync(4, _ =>
        {
            Interlocked.Increment(ref calls);
            return gate.Task;
        });
        var second = cache.GetOrAddAsync(4, _ =>
        {
            Interlocked.Increment(ref calls);
            return gate.Task;
        });
        gate.SetResult("page");

        (await Task.WhenAll(first, second)).Should().OnlyContain(value => value == "page");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task Prefetch_ignores_duplicate_and_out_of_range_pages()
    {
        using var cache = new ReportPageCache<int>(3);
        var requested = new ConcurrentBag<int>();

        await cache.PrefetchAsync(new[] { 0, 2, 2, 3, 4 }, 1, 3, page =>
        {
            requested.Add(page);
            return Task.FromResult(page);
        });

        requested.Should().BeEquivalentTo(new[] { 2, 3 });
        cache.CachedPages.Should().BeEquivalentTo(new[] { 2, 3 });
    }

    [Fact]
    public async Task Cancellation_cancels_wait_without_poisoning_shared_render()
    {
        using var cache = new ReportPageCache<int>(2);
        var gate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var task = cache.GetOrAddAsync(1, _ => gate.Task, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        gate.SetResult(7);
        (await cache.GetOrAddAsync(1, _ => Task.FromResult(8))).Should().Be(7);
    }
}
