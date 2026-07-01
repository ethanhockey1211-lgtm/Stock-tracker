using CommunityToolkit.Mvvm.ComponentModel;
using Tremor.Core.Models;

namespace Tremor.ViewModels;

/// <summary>
/// A single watchlist row. Live fields raise change notifications so the UI
/// updates in place as new ticks arrive. Displays observed data only.
/// </summary>
public partial class WatchlistRowViewModel : ObservableObject
{
    public WatchlistRowViewModel(Token token)
    {
        Token = token;
    }

    public Token Token { get; }

    public string Symbol => Token.Symbol;

    public string Pair => Token.Pair;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private decimal _priceChangePercent;

    [ObservableProperty]
    private decimal _quoteVolume;

    [ObservableProperty]
    private bool _hasData;

    public bool IsUp => PriceChangePercent >= 0;

    public void Update(PriceTick tick)
    {
        Price = tick.Price;
        PriceChangePercent = tick.PriceChangePercent24h;
        QuoteVolume = tick.QuoteVolume24h;
        HasData = true;
        OnPropertyChanged(nameof(IsUp));
    }
}
