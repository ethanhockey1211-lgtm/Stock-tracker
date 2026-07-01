using Tremor.Core.Models;
using Tremor.Core.Services.Persistence;
using Tremor.Core.Services.Watchlist;
using Xunit;

namespace Tremor.Core.Tests;

public class PersistentWatchlistServiceTests
{
    private static Token Doge => new() { Symbol = "DOGEUSDT", BaseAsset = "DOGE", QuoteAsset = "USDT" };

    [Fact]
    public async Task FirstRunSeedsStarterWatchlist()
    {
        var store = new InMemoryPreferencesStore();
        var service = new PersistentWatchlistService(store);

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, i => i.Symbol == "BTCUSDT");
        Assert.Contains(all, i => i.Symbol == "ETHUSDT");
    }

    [Fact]
    public async Task AddPersistsAcrossInstances()
    {
        var store = new InMemoryPreferencesStore();

        var service1 = new PersistentWatchlistService(store);
        await service1.AddAsync(Doge);

        var service2 = new PersistentWatchlistService(store);
        var all = await service2.GetAllAsync();

        Assert.Contains(all, i => i.Symbol == "DOGEUSDT");
    }

    [Fact]
    public async Task RemovePersistsAcrossInstances()
    {
        var store = new InMemoryPreferencesStore();

        var service1 = new PersistentWatchlistService(store);
        await service1.GetAllAsync(); // triggers seed of BTC/ETH
        await service1.RemoveAsync("BTCUSDT");

        var service2 = new PersistentWatchlistService(store);
        var all = await service2.GetAllAsync();

        Assert.DoesNotContain(all, i => i.Symbol == "BTCUSDT");
        Assert.Contains(all, i => i.Symbol == "ETHUSDT");
    }

    [Fact]
    public async Task AddIsIdempotent()
    {
        var store = new InMemoryPreferencesStore();
        var service = new PersistentWatchlistService(store);

        await service.AddAsync(Doge);
        await service.AddAsync(Doge);

        var all = await service.GetAllAsync();
        Assert.Single(all, i => i.Symbol == "DOGEUSDT");
    }

    [Fact]
    public async Task DoesNotReseedWhenStoredWatchlistIsEmptied()
    {
        var store = new InMemoryPreferencesStore();

        var service1 = new PersistentWatchlistService(store);
        await service1.GetAllAsync(); // seeds BTC/ETH
        await service1.RemoveAsync("BTCUSDT");
        await service1.RemoveAsync("ETHUSDT");

        // A fresh instance must respect the (now empty) stored list, not re-seed.
        var service2 = new PersistentWatchlistService(store);
        var all = await service2.GetAllAsync();

        Assert.Empty(all);
    }
}
