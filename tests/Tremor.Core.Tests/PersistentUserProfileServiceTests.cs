using Tremor.Core.Models;
using Tremor.Core.Services.Persistence;
using Tremor.Core.Services.Users;
using Xunit;

namespace Tremor.Core.Tests;

public class PersistentUserProfileServiceTests
{
    [Fact]
    public async Task DefaultsToFreeTierWithPushEnabled()
    {
        var store = new InMemoryPreferencesStore();
        var service = new PersistentUserProfileService(store);

        var profile = await service.GetAsync();

        Assert.Equal(SubscriptionTier.Free, profile.Tier);
        Assert.True(profile.PushNotificationsEnabled);
    }

    [Fact]
    public async Task SavePersistsAcrossInstances()
    {
        var store = new InMemoryPreferencesStore();

        var service1 = new PersistentUserProfileService(store);
        var profile = await service1.GetAsync();
        profile.Tier = SubscriptionTier.Premium;
        profile.PushNotificationsEnabled = false;
        await service1.SaveAsync(profile);

        var service2 = new PersistentUserProfileService(store);
        var reloaded = await service2.GetAsync();

        Assert.Equal(SubscriptionTier.Premium, reloaded.Tier);
        Assert.True(reloaded.IsPremium);
        Assert.False(reloaded.PushNotificationsEnabled);
    }
}
