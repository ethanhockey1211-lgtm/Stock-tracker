namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// A minimal string key/value store used for local persistence (watchlist, user
/// profile). The MAUI head backs this with a file in the app data directory; the
/// default in-memory implementation keeps Core usable standalone and in tests.
/// </summary>
public interface IPreferencesStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
