namespace Tremor.Core.Models;

/// <summary>
/// A tradeable crypto instrument the app knows about (a symbol on an exchange).
/// </summary>
public sealed record Token
{
    /// <summary>Exchange trading symbol, e.g. "BTCUSDT".</summary>
    public required string Symbol { get; init; }

    /// <summary>Base asset, e.g. "BTC".</summary>
    public required string BaseAsset { get; init; }

    /// <summary>Quote asset, e.g. "USDT".</summary>
    public required string QuoteAsset { get; init; }

    /// <summary>Optional human-friendly display name.</summary>
    public string? DisplayName { get; init; }

    public string Pair => $"{BaseAsset}/{QuoteAsset}";
}
