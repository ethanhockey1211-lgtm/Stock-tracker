using Tremor.Core.Models;
using Tremor.Core.Services.Watchlist;
using Xunit;

namespace Tremor.Core.Tests;

public class WatchlistServiceTests
{
    private static Token Btc => new() { Symbol = "BTCUSDT", BaseAsset = "BTC", QuoteAsset = "USDT" };
    private static Token Eth => new() { Symbol = "ETHUSDT", BaseAsset = "ETH", QuoteAsset = "USDT" };

    [Fact]
    public async Task AddThenGetReturnsItem()
    {
        var service = new InMemoryWatchlistService();
        await service.AddAsync(Btc);

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("BTCUSDT", all[0].Symbol);
    }

    [Fact]
    public async Task AddIsIdempotentBySymbol()
    {
        var service = new InMemoryWatchlistService();
        await service.AddAsync(Btc);
        await service.AddAsync(Btc);

        var all = await service.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task RemoveDropsItem()
    {
        var service = new InMemoryWatchlistService();
        await service.AddAsync(Btc);
        await service.AddAsync(Eth);
        await service.RemoveAsync("BTCUSDT");

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("ETHUSDT", all[0].Symbol);
    }

    [Fact]
    public async Task ContainsIsCaseInsensitive()
    {
        var service = new InMemoryWatchlistService();
        await service.AddAsync(Btc);

        Assert.True(await service.ContainsAsync("btcusdt"));
        Assert.False(await service.ContainsAsync("dogeusdt"));
    }

    [Fact]
    public async Task ChangedRaisedOnAddAndRemove()
    {
        var service = new InMemoryWatchlistService();
        var count = 0;
        service.Changed += (_, _) => count++;

        await service.AddAsync(Btc);
        await service.RemoveAsync("BTCUSDT");

        Assert.Equal(2, count);
    }
}
