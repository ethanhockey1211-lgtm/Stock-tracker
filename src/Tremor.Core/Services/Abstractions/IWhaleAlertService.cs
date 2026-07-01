using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Surfaces large on-chain movements (exchange inflows/outflows, big transfers)
/// for tracked assets. The MVP implementation is a placeholder that models the
/// interface a real on-chain provider (Etherscan/Moralis) would satisfy.
/// </summary>
public interface IWhaleAlertService
{
    /// <summary>Fetch recent detected whale movements for the given base assets.</summary>
    Task<IReadOnlyList<WhaleAlert>> GetRecentAsync(
        IEnumerable<string> baseAssets,
        CancellationToken cancellationToken = default);
}
