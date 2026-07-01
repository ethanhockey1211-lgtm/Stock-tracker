using System.Diagnostics;

namespace Tremor.Core.Models;

/// <summary>
/// A token a user is tracking, plus its most recently observed tick.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class WatchlistItem
{
    public required Token Token { get; init; }

    /// <summary>When the user added this token to their watchlist (UTC).</summary>
    public DateTimeOffset AddedUtc { get; init; }

    /// <summary>Latest observed market snapshot, if one has arrived yet.</summary>
    public PriceTick? LatestTick { get; set; }

    public string Symbol => Token.Symbol;

    private string DebuggerDisplay => $"{Symbol} ({LatestTick?.Price ?? 0})";
}
