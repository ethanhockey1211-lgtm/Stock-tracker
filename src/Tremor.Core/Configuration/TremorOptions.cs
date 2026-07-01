namespace Tremor.Core.Configuration;

/// <summary>
/// Tunable settings for data ingestion and detection. Bound from configuration
/// at startup; sensible defaults let the app run with zero configuration.
/// </summary>
public sealed class TremorOptions
{
    public const string SectionName = "Tremor";

    /// <summary>Binance REST base URL for public market data (no API key required).</summary>
    public string BinanceRestBaseUrl { get; set; } = "https://api.binance.com";

    /// <summary>Binance combined-stream WebSocket base URL for public market data.</summary>
    public string BinanceWebSocketBaseUrl { get; set; } = "wss://stream.binance.com:9443";

    /// <summary>How often to refresh listing/market snapshots when not streaming.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>Volume-spike detection parameters.</summary>
    public VolumeSpikeOptions VolumeSpike { get; set; } = new();

    /// <summary>Whale-movement detection parameters.</summary>
    public WhaleOptions Whale { get; set; } = new();

    /// <summary>Affiliate/referral deep-link settings (placeholder for the MVP).</summary>
    public AffiliateOptions Affiliate { get; set; } = new();
}

public sealed class VolumeSpikeOptions
{
    /// <summary>Number of recent samples that form the rolling baseline.</summary>
    public int BaselineWindow { get; set; } = 20;

    /// <summary>Minimum samples required before a spike can be reported.</summary>
    public int MinSamples { get; set; } = 5;

    /// <summary>A sample this many times the baseline (or more) is a spike. 3.0 == 3x.</summary>
    public decimal SpikeMultiple { get; set; } = 3.0m;

    /// <summary>At/above this multiple the spike is flagged High severity.</summary>
    public decimal HighSeverityMultiple { get; set; } = 6.0m;
}

public sealed class WhaleOptions
{
    /// <summary>Minimum USD-equivalent size for a transfer to count as a whale move.</summary>
    public decimal MinUsdValue { get; set; } = 1_000_000m;

    /// <summary>At/above this USD-equivalent size the move is flagged High severity.</summary>
    public decimal HighSeverityUsdValue { get; set; } = 10_000_000m;
}

public sealed class AffiliateOptions
{
    /// <summary>
    /// Deep-link template used by "View on exchange" buttons. {symbol} is replaced
    /// with the trading symbol. This routes the user out to an exchange; Tremor
    /// never executes a trade in-app. Replace the ref code before shipping.
    /// </summary>
    public string ExchangeLinkTemplate { get; set; } =
        "https://accounts.binance.com/register?ref=TREMOR_PLACEHOLDER&symbol={symbol}";
}
