using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Market;

/// <summary>
/// Live market data from Binance public endpoints — no API key required, since
/// only public market data is read. REST is used for one-off snapshots; a
/// combined WebSocket stream is used for live updates. Read-only by design.
/// </summary>
public sealed class BinanceMarketDataService : IMarketDataService
{
    private readonly HttpClient _http;
    private readonly TremorOptions _options;
    private readonly ILogger<BinanceMarketDataService> _logger;

    public BinanceMarketDataService(
        HttpClient http,
        IOptions<TremorOptions> options,
        ILogger<BinanceMarketDataService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(_options.BinanceRestBaseUrl);
        }
    }

    public async Task<PriceTick?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        try
        {
            var dto = await _http.GetFromJsonAsync<BinanceTicker24hDto>(
                $"/api/v3/ticker/24hr?symbol={Uri.EscapeDataString(symbol.ToUpperInvariant())}",
                cancellationToken).ConfigureAwait(false);

            return dto is null ? null : MapRest(dto);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch ticker for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<IReadOnlyList<PriceTick>> GetTickersAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken = default)
    {
        var list = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.ToUpperInvariant())
            .Distinct()
            .ToArray();

        if (list.Length == 0)
        {
            return [];
        }

        // Binance expects a JSON array as the `symbols` query value.
        var symbolsJson = JsonSerializer.Serialize(list);

        try
        {
            var dtos = await _http.GetFromJsonAsync<List<BinanceTicker24hDto>>(
                $"/api/v3/ticker/24hr?symbols={Uri.EscapeDataString(symbolsJson)}",
                cancellationToken).ConfigureAwait(false);

            if (dtos is null)
            {
                return [];
            }

            return dtos.Select(MapRest).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch tickers for {Count} symbols", list.Length);
            return [];
        }
    }

    public async IAsyncEnumerable<PriceTick> StreamTickersAsync(
        IReadOnlyCollection<string> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var streams = symbols
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => $"{s.ToLowerInvariant()}@ticker")
            .Distinct()
            .ToArray();

        if (streams.Length == 0)
        {
            yield break;
        }

        var url = $"{_options.BinanceWebSocketBaseUrl.TrimEnd('/')}/stream?streams={string.Join('/', streams)}";

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(url), cancellationToken).ConfigureAwait(false);

        var buffer = new byte[16 * 1024];
        var messageBuilder = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            messageBuilder.Clear();
            WebSocketReceiveResult result;

            do
            {
                try
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
                catch (WebSocketException ex)
                {
                    _logger.LogWarning(ex, "WebSocket receive failed; ending stream");
                    yield break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    yield break;
                }

                messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            var tick = ParseStreamMessage(messageBuilder.ToString());
            if (tick is not null)
            {
                yield return tick;
            }
        }
    }

    private PriceTick? ParseStreamMessage(string json)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<BinanceStreamEnvelope>(json);
            var data = envelope?.Data;
            if (data?.Symbol is null)
            {
                return null;
            }

            return new PriceTick
            {
                Symbol = data.Symbol,
                Price = NumberParsing.ToDecimal(data.LastPrice),
                PriceChangePercent24h = NumberParsing.ToDecimal(data.PriceChangePercent),
                Volume24h = NumberParsing.ToDecimal(data.Volume),
                QuoteVolume24h = NumberParsing.ToDecimal(data.QuoteVolume),
                TimestampUtc = data.EventTimeMs > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(data.EventTimeMs)
                    : DateTimeOffset.UtcNow,
            };
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Skipping unparseable stream message");
            return null;
        }
    }

    private static PriceTick MapRest(BinanceTicker24hDto dto) => new()
    {
        Symbol = dto.Symbol ?? string.Empty,
        Price = NumberParsing.ToDecimal(dto.LastPrice),
        PriceChangePercent24h = NumberParsing.ToDecimal(dto.PriceChangePercent),
        Volume24h = NumberParsing.ToDecimal(dto.Volume),
        QuoteVolume24h = NumberParsing.ToDecimal(dto.QuoteVolume),
        TimestampUtc = DateTimeOffset.UtcNow,
    };
}
