using System.Text.Json;
using System.Text.Json.Serialization;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Watchlist;

/// <summary>
/// Watchlist backed by an <see cref="IPreferencesStore"/> so tokens survive app
/// restarts. Holds an in-memory cache loaded on first use and persists on every
/// change. On first ever run (no stored watchlist) it seeds a small starter set
/// so the app isn't empty.
/// </summary>
public sealed class PersistentWatchlistService : IWatchlistService
{
    private const string StorageKey = "watchlist.v1";

    private static readonly WatchlistEntry[] SeedTokens =
    [
        new("BTCUSDT", "BTC", "USDT", null),
        new("ETHUSDT", "ETH", "USDT", null),
    ];

    private readonly IPreferencesStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Dictionary<string, WatchlistItem>? _items;

    public event EventHandler? Changed;

    public PersistentWatchlistService(IPreferencesStore store, TimeProvider? timeProvider = null)
    {
        _store = store;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (items)
        {
            return items.Values.OrderBy(i => i.AddedUtc).ToList();
        }
    }

    public async Task<WatchlistItem> AddAsync(Token token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        var items = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        WatchlistItem item;
        bool added;
        lock (items)
        {
            if (items.TryGetValue(token.Symbol, out var existing))
            {
                return existing;
            }

            item = new WatchlistItem { Token = token, AddedUtc = _timeProvider.GetUtcNow() };
            items[token.Symbol] = item;
            added = true;
        }

        if (added)
        {
            await PersistAsync(items, cancellationToken).ConfigureAwait(false);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return item;
    }

    public async Task RemoveAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var items = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        bool removed;
        lock (items)
        {
            removed = items.Remove(symbol);
        }

        if (removed)
        {
            await PersistAsync(items, cancellationToken).ConfigureAwait(false);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<bool> ContainsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        var items = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        lock (items)
        {
            return items.ContainsKey(symbol);
        }
    }

    private async Task<Dictionary<string, WatchlistItem>> EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_items is not null)
        {
            return _items;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_items is not null)
            {
                return _items;
            }

            var json = await _store.GetAsync(StorageKey, cancellationToken).ConfigureAwait(false);
            if (json is null)
            {
                // First run: seed and persist a starter watchlist.
                var seeded = BuildFromEntries(SeedTokens);
                _items = seeded;
                await PersistAsync(seeded, cancellationToken).ConfigureAwait(false);
                return seeded;
            }

            var entries = Deserialize(json);
            _items = BuildFromEntries(entries);
            return _items;
        }
        finally
        {
            _gate.Release();
        }
    }

    private Dictionary<string, WatchlistItem> BuildFromEntries(IEnumerable<WatchlistEntry> entries)
    {
        var map = new Dictionary<string, WatchlistItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var token = new Token
            {
                Symbol = entry.Symbol,
                BaseAsset = entry.BaseAsset,
                QuoteAsset = entry.QuoteAsset,
                DisplayName = entry.DisplayName,
            };
            map[entry.Symbol] = new WatchlistItem
            {
                Token = token,
                AddedUtc = entry.AddedUtc ?? _timeProvider.GetUtcNow(),
            };
        }

        return map;
    }

    private async Task PersistAsync(Dictionary<string, WatchlistItem> items, CancellationToken cancellationToken)
    {
        List<WatchlistEntry> entries;
        lock (items)
        {
            entries = items.Values
                .OrderBy(i => i.AddedUtc)
                .Select(i => new WatchlistEntry(
                    i.Token.Symbol, i.Token.BaseAsset, i.Token.QuoteAsset, i.Token.DisplayName, i.AddedUtc))
                .ToList();
        }

        var json = JsonSerializer.Serialize(entries);
        await _store.SetAsync(StorageKey, json, cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<WatchlistEntry> Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<WatchlistEntry>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record WatchlistEntry(
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("base")] string BaseAsset,
        [property: JsonPropertyName("quote")] string QuoteAsset,
        [property: JsonPropertyName("name")] string? DisplayName,
        [property: JsonPropertyName("added")] DateTimeOffset? AddedUtc = null);
}
