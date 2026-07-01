using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Manages the user's tracked tokens. Persistence is an implementation detail
/// (in-memory for the MVP; swap for local storage or a backend later).
/// </summary>
public interface IWatchlistService
{
    /// <summary>Raised whenever the set of watched tokens changes.</summary>
    event EventHandler? Changed;

    Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WatchlistItem> AddAsync(Token token, CancellationToken cancellationToken = default);

    Task RemoveAsync(string symbol, CancellationToken cancellationToken = default);

    Task<bool> ContainsAsync(string symbol, CancellationToken cancellationToken = default);
}
