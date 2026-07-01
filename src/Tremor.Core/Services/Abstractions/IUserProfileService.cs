using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Loads and persists the local user profile, including the (unenforced)
/// Free/Premium tier flag and notification preference.
/// </summary>
public interface IUserProfileService
{
    Task<UserProfile> GetAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
