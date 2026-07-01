using System.Collections.Concurrent;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Persistence;

/// <summary>In-memory <see cref="IPreferencesStore"/>. Default for Core/tests.</summary>
public sealed class InMemoryPreferencesStore : IPreferencesStore
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.Ordinal);

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _values.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
