using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Tremor.Core.Copy;
using Tremor.Core.Services.Abstractions;
using Tremor.Core.Services.Market;

namespace Tremor.ViewModels;

/// <summary>
/// Detail view for a single token: live snapshot plus an outbound "View on
/// exchange" action. Read-only and factual — no prediction, no recommendation.
/// </summary>
[QueryProperty(nameof(Symbol), "symbol")]
public partial class TokenDetailViewModel : BaseViewModel
{
    private readonly IMarketDataService _marketData;
    private readonly IAffiliateLinkService _affiliate;
    private readonly ILogger<TokenDetailViewModel> _logger;

    [ObservableProperty]
    private string _symbol = string.Empty;

    [ObservableProperty]
    private string _pair = string.Empty;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private decimal _priceChangePercent;

    [ObservableProperty]
    private decimal _quoteVolume;

    [ObservableProperty]
    private bool _hasData;

    public string ViewOnExchangeText => AppCopy.ViewOnExchange;

    public string ViewOnExchangeNote => AppCopy.ViewOnExchangeNote;

    public string Disclaimer => AppCopy.Disclaimer;

    public TokenDetailViewModel(
        IMarketDataService marketData,
        IAffiliateLinkService affiliate,
        ILogger<TokenDetailViewModel> logger)
    {
        _marketData = marketData;
        _affiliate = affiliate;
        _logger = logger;
    }

    partial void OnSymbolChanged(string value)
    {
        Title = value;
        if (SymbolParser.TryParse(value, out var token))
        {
            Pair = token.Pair;
        }

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol) || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var tick = await _marketData.GetTickerAsync(Symbol).ConfigureAwait(false);
            if (tick is null)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Price = tick.Price;
                PriceChangePercent = tick.PriceChangePercent24h;
                QuoteVolume = tick.QuoteVolume24h;
                HasData = true;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load detail for {Symbol}", Symbol);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync().ConfigureAwait(false);

    /// <summary>
    /// Opens the token on an exchange via an outbound referral link. Tremor never
    /// executes a trade in-app and never holds funds.
    /// </summary>
    [RelayCommand]
    private async Task ViewOnExchangeAsync()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            return;
        }

        try
        {
            var uri = _affiliate.BuildExchangeLink(Symbol);
            await Launcher.Default.OpenAsync(uri).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open exchange link for {Symbol}", Symbol);
        }
    }
}
