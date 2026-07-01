using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Detects newly tradeable symbols on an exchange by diffing the current symbol
/// universe against the previously seen one.
/// </summary>
public interface INewListingService
{
    /// <summary>
    /// Return listings detected as new since the previous call. The first call
    /// establishes the baseline and returns an empty list (nothing is "new" yet).
    /// </summary>
    Task<IReadOnlyList<TokenListing>> PollNewListingsAsync(CancellationToken cancellationToken = default);
}
