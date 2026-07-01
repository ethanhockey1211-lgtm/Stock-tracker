using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tremor.Core.Copy;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;
using Tremor.Core.Services.Market;

namespace Tremor.ViewModels;

/// <summary>
/// Backs the watchlist screen: the user's tracked tokens with live price/volume,
/// plus add/remove and an outbound "View on exchange" action.
/// </summary>
public partial class WatchlistViewModel : BaseViewModel
{
    private readonly IWatchlistService _watchlist;
    private readonly IMarketDataService _marketData;
    private readonly IAffiliateLinkService _affiliate;
    private readonly ILogger<WatchlistViewModel> _logger;

    private CancellationTokenSource? _pollCts;

    public ObservableCollection<WatchlistRowViewModel> Items { get; } = [];

    [ObservableProperty]
    private string _newSymbol = string.Empty;

    [ObservableProperty]
    private string? _addError;

    public string EmptyMessage => AppCopy.WatchlistEmpty;

    public WatchlistViewModel(
        IWatchlistService watchlist,
        IMarketDataService marketData,
        IAffiliateLinkService affiliate,
        ILogger<WatchlistViewModel> logger)
    {
        Title = AppCopy.WatchlistTitle;
        _watchlist = watchlist;
        _marketData = marketData;
        _affiliate = affiliate;
        _logger = logger;
    }

    public bool IsEmpty => Items.Count == 0;

    [RelayCommand]
    private async Task AppearingAsync()
    {
        await LoadAsync().ConfigureAwait(false);
        StartPolling();
    }

    [RelayCommand]
    private void Disappearing()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    private async Task LoadAsync()
    {
        var items = await _watchlist.GetAllAsync().ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Items.Clear();
            foreach (var item in items)
            {
                var row = new WatchlistRowViewModel(item.Token);
                if (item.LatestTick is not null)
                {
                    row.Update(item.LatestTick);
                }

                Items.Add(row);
            }

            OnPropertyChanged(nameof(IsEmpty));
        }).ConfigureAwait(false);

        await RefreshPricesAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        AddError = null;

        if (!SymbolParser.TryParse(NewSymbol, out var token))
        {
            AddError = "Enter a valid pair, e.g. BTCUSDT or BTC/USDT.";
            return;
        }

        if (await _watchlist.ContainsAsync(token.Symbol).ConfigureAwait(false))
        {
            AddError = $"{token.Symbol} is already on your watchlist.";
            return;
        }

        await _watchlist.AddAsync(token).ConfigureAwait(false);
        NewSymbol = string.Empty;
        await LoadAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task RemoveAsync(WatchlistRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        await _watchlist.RemoveAsync(row.Symbol).ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            Items.Remove(row);
            OnPropertyChanged(nameof(IsEmpty));
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Opens the token on an exchange via an outbound referral link. Tremor never
    /// executes a trade in-app and never holds funds.
    /// </summary>
    [RelayCommand]
    private async Task ViewOnExchangeAsync(WatchlistRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        try
        {
            var uri = _affiliate.BuildExchangeLink(row.Symbol);
            await Launcher.Default.OpenAsync(uri).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open exchange link for {Symbol}", row.Symbol);
        }
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await RefreshPricesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Screen left; stop quietly.
        }
    }

    private async Task RefreshPricesAsync(CancellationToken cancellationToken = default)
    {
        var symbols = Items.Select(i => i.Symbol).ToArray();
        if (symbols.Length == 0)
        {
            return;
        }

        var ticks = await _marketData.GetTickersAsync(symbols, cancellationToken).ConfigureAwait(false);
        if (ticks.Count == 0)
        {
            return;
        }

        var bySymbol = ticks.ToDictionary(t => t.Symbol, StringComparer.OrdinalIgnoreCase);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            foreach (var row in Items)
            {
                if (bySymbol.TryGetValue(row.Symbol, out var tick))
                {
                    row.Update(tick);
                }
            }
        }).ConfigureAwait(false);
    }
}
