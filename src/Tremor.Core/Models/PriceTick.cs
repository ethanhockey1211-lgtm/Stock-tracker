namespace Tremor.Core.Models;

/// <summary>
/// A single snapshot of an instrument's market state at a point in time.
/// Purely descriptive of what has already been observed — never a forecast.
/// </summary>
public sealed record PriceTick
{
    /// <summary>Exchange trading symbol, e.g. "BTCUSDT".</summary>
    public required string Symbol { get; init; }

    /// <summary>Last traded price.</summary>
    public decimal Price { get; init; }

    /// <summary>Rolling 24h price-change percentage as reported by the exchange.</summary>
    public decimal PriceChangePercent24h { get; init; }

    /// <summary>Rolling 24h base-asset volume.</summary>
    public decimal Volume24h { get; init; }

    /// <summary>Rolling 24h quote-asset volume (e.g. USDT turnover).</summary>
    public decimal QuoteVolume24h { get; init; }

    /// <summary>Time the tick was observed (UTC).</summary>
    public DateTimeOffset TimestampUtc { get; init; }
}
