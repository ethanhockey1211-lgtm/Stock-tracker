using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Streams and fetches live market data for trading symbols. Read-only:
/// it observes the market, it never places orders.
/// </summary>
public interface IMarketDataService
{
    /// <summary>Fetch a one-off snapshot for a single symbol.</summary>
    Task<PriceTick?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Fetch snapshots for several symbols at once.</summary>
    Task<IReadOnlyList<PriceTick>> GetTickersAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to a live stream of ticks for the given symbols. The returned
    /// async stream yields a tick whenever the exchange pushes an update.
    /// </summary>
    IAsyncEnumerable<PriceTick> StreamTickersAsync(
        IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken = default);
}
