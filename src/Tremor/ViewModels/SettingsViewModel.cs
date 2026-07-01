using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tremor.Core.Copy;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.ViewModels;

/// <summary>
/// Settings: notification toggle and the (unenforced) Free/Premium tier flag.
/// Premium is scaffolding only — no billing or gating exists in the MVP.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IUserProfileService _profiles;
    private UserProfile? _profile;

    [ObservableProperty]
    private bool _pushNotificationsEnabled;

    [ObservableProperty]
    private bool _isPremium;

    public string TierLabel => IsPremium ? AppCopy.TierLabelPremium : AppCopy.TierLabelFree;

    public string Disclaimer => AppCopy.Disclaimer;

    public string Version => $"{AppCopy.AppName} {AppInfo.Current.VersionString}";

    public SettingsViewModel(IUserProfileService profiles)
    {
        Title = AppCopy.SettingsTitle;
        _profiles = profiles;
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        _profile = await _profiles.GetAsync().ConfigureAwait(false);
        PushNotificationsEnabled = _profile.PushNotificationsEnabled;
        IsPremium = _profile.IsPremium;
        OnPropertyChanged(nameof(TierLabel));
    }

    partial void OnPushNotificationsEnabledChanged(bool value)
    {
        if (_profile is null)
        {
            return;
        }

        _profile.PushNotificationsEnabled = value;
        _ = _profiles.SaveAsync(_profile);
    }

    /// <summary>
    /// Dev-only tier toggle so the scaffolded Free/Premium flag can be exercised.
    /// Real Premium is gated by billing in Phase 2+, not by this switch.
    /// </summary>
    partial void OnIsPremiumChanged(bool value)
    {
        if (_profile is null)
        {
            return;
        }

        _profile.Tier = value ? SubscriptionTier.Premium : SubscriptionTier.Free;
        OnPropertyChanged(nameof(TierLabel));
        _ = _profiles.SaveAsync(_profile);
    }
}
