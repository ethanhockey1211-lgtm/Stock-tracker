using System.Text.Json;
using System.Text.Json.Serialization;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Users;

/// <summary>
/// User profile backed by an <see cref="IPreferencesStore"/> so the notification
/// preference and (unenforced) Free/Premium tier survive restarts.
/// </summary>
public sealed class PersistentUserProfileService : IUserProfileService
{
    private const string StorageKey = "userprofile.v1";

    private readonly IPreferencesStore _store;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private UserProfile? _profile;

    public PersistentUserProfileService(IPreferencesStore store)
    {
        _store = store;
    }

    public async Task<UserProfile> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_profile is not null)
        {
            return _profile;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_profile is not null)
            {
                return _profile;
            }

            var json = await _store.GetAsync(StorageKey, cancellationToken).ConfigureAwait(false);
            _profile = json is null ? CreateDefault() : Deserialize(json);
            return _profile;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profile = profile;

        var dto = new ProfileDto(profile.Id, profile.DisplayName, profile.Tier, profile.PushNotificationsEnabled);
        var json = JsonSerializer.Serialize(dto);
        await _store.SetAsync(StorageKey, json, cancellationToken).ConfigureAwait(false);
    }

    private static UserProfile CreateDefault() => new()
    {
        Id = "local-user",
        Tier = SubscriptionTier.Free,
    };

    private static UserProfile Deserialize(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<ProfileDto>(json);
            if (dto is null)
            {
                return CreateDefault();
            }

            return new UserProfile
            {
                Id = string.IsNullOrWhiteSpace(dto.Id) ? "local-user" : dto.Id,
                DisplayName = dto.DisplayName,
                Tier = dto.Tier,
                PushNotificationsEnabled = dto.PushNotificationsEnabled,
            };
        }
        catch (JsonException)
        {
            return CreateDefault();
        }
    }

    private sealed record ProfileDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? DisplayName,
        [property: JsonPropertyName("tier")] SubscriptionTier Tier,
        [property: JsonPropertyName("push")] bool PushNotificationsEnabled);
}
