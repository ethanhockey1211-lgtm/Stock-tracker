using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tremor.Core.Copy;
using Tremor.Core.Models;
using Tremor.Core.Services.Scanning;

namespace Tremor.ViewModels;

/// <summary>
/// Live feed of detected events (volume spikes, whale moves, new listings).
/// Read-only, factual, no recommendations — the disclaimer is always shown.
/// </summary>
public partial class AlertsViewModel : BaseViewModel
{
    private readonly IAlertSink _alertSink;

    public ObservableCollection<Alert> Alerts { get; } = [];

    public string EmptyMessage => AppCopy.AlertsEmpty;

    public string Disclaimer => AppCopy.Disclaimer;

    public AlertsViewModel(IAlertSink alertSink)
    {
        Title = AppCopy.AlertsTitle;
        _alertSink = alertSink;
    }

    public bool IsEmpty => Alerts.Count == 0;

    [RelayCommand]
    private void Appearing()
    {
        Alerts.Clear();
        foreach (var alert in _alertSink.Recent)
        {
            Alerts.Add(alert);
        }

        OnPropertyChanged(nameof(IsEmpty));
        _alertSink.AlertPublished += OnAlertPublished;
    }

    [RelayCommand]
    private void Disappearing()
    {
        _alertSink.AlertPublished -= OnAlertPublished;
    }

    private void OnAlertPublished(object? sender, Alert alert)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Alerts.Insert(0, alert);
            OnPropertyChanged(nameof(IsEmpty));
        });
    }
}
