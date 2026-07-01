using System.Collections.Concurrent;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Watchlist;

/// <summary>
/// In-memory watchlist for the MVP. Deliberately simple: swap for a persisted
/// store (local storage / SQLite / backend) later without touching callers.
/// </summary>
public sealed class InMemoryWatchlistService : IWatchlistService
{
    private readonly ConcurrentDictionary<string, WatchlistItem> _items = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider _timeProvider;

    public event EventHandler? Changed;

    public InMemoryWatchlistService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WatchlistItem> snapshot = _items.Values
            .OrderBy(i => i.AddedUtc)
            .ToList();
        return Task.FromResult(snapshot);
    }

    public Task<WatchlistItem> AddAsync(Token token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        var item = _items.GetOrAdd(token.Symbol, _ => new WatchlistItem
        {
            Token = token,
            AddedUtc = _timeProvider.GetUtcNow(),
        });

        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(item);
    }

    public Task RemoveAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        if (_items.TryRemove(symbol, out _))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ContainsAsync(string symbol, CancellationToken cancellationToken = default)
        => Task.FromResult(!string.IsNullOrWhiteSpace(symbol) && _items.ContainsKey(symbol));
}
