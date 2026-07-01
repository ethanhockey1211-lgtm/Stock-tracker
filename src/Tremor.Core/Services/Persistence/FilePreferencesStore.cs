using System.Text.Json;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Persistence;

/// <summary>
/// File-backed <see cref="IPreferencesStore"/> that keeps all keys in a single
/// JSON document under the given directory. Small data, loaded once and rewritten
/// on change; access is serialized so concurrent callers stay consistent.
/// </summary>
public sealed class FilePreferencesStore : IPreferencesStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<string, string>? _cache;

    public FilePreferencesStore(string directoryPath, string fileName = "tremor.prefs.json")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        Directory.CreateDirectory(directoryPath);
        _filePath = Path.Combine(directoryPath, fileName);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cache = await LoadAsync(cancellationToken).ConfigureAwait(false);
            return cache.TryGetValue(key, out var value) ? value : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cache = await LoadAsync(cancellationToken).ConfigureAwait(false);
            cache[key] = value;
            await SaveAsync(cache, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cache = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (cache.Remove(key))
            {
                await SaveAsync(cache, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        if (!File.Exists(_filePath))
        {
            _cache = new Dictionary<string, string>(StringComparer.Ordinal);
            return _cache;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer
                .DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            _cache = loaded ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Corrupt or unreadable store: start clean rather than crash.
            _cache = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return _cache;
    }

    private async Task SaveAsync(Dictionary<string, string> cache, CancellationToken cancellationToken)
    {
        // Write to a temp file then move, so a crash mid-write can't corrupt the store.
        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, cache, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }
}
