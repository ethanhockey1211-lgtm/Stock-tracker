using System.Text.Json.Serialization;

namespace Tremor.Core.Services.Market;

/// <summary>
/// Binance REST /api/v3/ticker/24hr payload. Binance returns numbers as strings.
/// </summary>
internal sealed class BinanceTicker24hDto
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("lastPrice")]
    public string? LastPrice { get; set; }

    [JsonPropertyName("priceChangePercent")]
    public string? PriceChangePercent { get; set; }

    [JsonPropertyName("volume")]
    public string? Volume { get; set; }

    [JsonPropertyName("quoteVolume")]
    public string? QuoteVolume { get; set; }
}

/// <summary>
/// Binance combined-stream envelope: { "stream": "...", "data": { ... } }.
/// </summary>
internal sealed class BinanceStreamEnvelope
{
    [JsonPropertyName("stream")]
    public string? Stream { get; set; }

    [JsonPropertyName("data")]
    public BinanceTickerStreamDto? Data { get; set; }
}

/// <summary>
/// Binance &lt;symbol&gt;@ticker stream payload (24hr rolling window ticker).
/// </summary>
internal sealed class BinanceTickerStreamDto
{
    [JsonPropertyName("s")]
    public string? Symbol { get; set; }

    [JsonPropertyName("c")]
    public string? LastPrice { get; set; }

    [JsonPropertyName("P")]
    public string? PriceChangePercent { get; set; }

    [JsonPropertyName("v")]
    public string? Volume { get; set; }

    [JsonPropertyName("q")]
    public string? QuoteVolume { get; set; }

    [JsonPropertyName("E")]
    public long EventTimeMs { get; set; }
}

/// <summary>Subset of Binance /api/v3/exchangeInfo used for listing discovery.</summary>
internal sealed class BinanceExchangeInfoDto
{
    [JsonPropertyName("symbols")]
    public List<BinanceSymbolDto> Symbols { get; set; } = [];
}

internal sealed class BinanceSymbolDto
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("baseAsset")]
    public string? BaseAsset { get; set; }

    [JsonPropertyName("quoteAsset")]
    public string? QuoteAsset { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
