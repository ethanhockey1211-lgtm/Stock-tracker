using System.Diagnostics;

namespace Tremor.Core.Models;

/// <summary>
/// A newly available trading symbol detected on an exchange or DEX.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed record TokenListing
{
    public required string Symbol { get; init; }

    public required string BaseAsset { get; init; }

    public required string QuoteAsset { get; init; }

    /// <summary>Where the listing was detected, e.g. "Binance".</summary>
    public required string Venue { get; init; }

    /// <summary>When Tremor first observed the symbol as tradeable (UTC).</summary>
    public DateTimeOffset DetectedUtc { get; init; }

    public string Pair => $"{BaseAsset}/{QuoteAsset}";

    private string DebuggerDisplay => $"{Symbol} on {Venue}";
}
