using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Users;

/// <summary>
/// In-memory user profile for the MVP. The Free/Premium tier flag exists but is
/// not gated anywhere yet (billing/paywall is Phase 2+). Replace with a persisted
/// store when accounts land.
/// </summary>
public sealed class InMemoryUserProfileService : IUserProfileService
{
    private UserProfile _profile = new()
    {
        Id = "local-user",
        Tier = SubscriptionTier.Free,
    };

    public Task<UserProfile> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_profile);

    public Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profile = profile;
        return Task.CompletedTask;
    }
}
