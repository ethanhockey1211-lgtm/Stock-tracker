namespace Tremor.Core.Models;

/// <summary>
/// Subscription tier. Scaffolding only — Premium is not priced or gated yet.
/// See Phase 2+ scope: actual billing/paywall enforcement is deferred.
/// </summary>
public enum SubscriptionTier
{
    Free,
    Premium,
}

/// <summary>
/// The local user of the app. Tremor never holds funds and never trades on the
/// user's behalf, so this model deliberately carries no keys, balances, or
/// exchange credentials — only preferences and the (unenforced) tier flag.
/// </summary>
public sealed class UserProfile
{
    public required string Id { get; init; }

    public string? DisplayName { get; set; }

    /// <summary>Free/Premium flag. Present for future gating; not enforced in the MVP.</summary>
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;

    /// <summary>Whether push notifications are enabled for detected events.</summary>
    public bool PushNotificationsEnabled { get; set; } = true;

    public bool IsPremium => Tier == SubscriptionTier.Premium;
}
