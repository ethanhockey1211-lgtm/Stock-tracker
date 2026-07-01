using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;
using Tremor.Core.Services.Market;

namespace Tremor.Core.Services.Listings;

/// <summary>
/// Detects new Binance listings by diffing the live symbol universe (from
/// /api/v3/exchangeInfo) against the set seen on the previous poll. The first
/// poll only establishes the baseline, so nothing is reported as "new" then.
/// </summary>
public sealed class BinanceNewListingService : INewListingService
{
    private readonly HttpClient _http;
    private readonly TremorOptions _options;
    private readonly ILogger<BinanceNewListingService> _logger;
    private readonly TimeProvider _timeProvider;

    private HashSet<string>? _knownSymbols;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public BinanceNewListingService(
        HttpClient http,
        IOptions<TremorOptions> options,
        ILogger<BinanceNewListingService> logger,
        TimeProvider? timeProvider = null)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(_options.BinanceRestBaseUrl);
        }
    }

    public async Task<IReadOnlyList<TokenListing>> PollNewListingsAsync(CancellationToken cancellationToken = default)
    {
        BinanceExchangeInfoDto? info;
        try
        {
            info = await _http.GetFromJsonAsync<BinanceExchangeInfoDto>(
                "/api/v3/exchangeInfo",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange info for listing detection");
            return [];
        }

        if (info is null)
        {
            return [];
        }

        var tradeable = info.Symbols
            .Where(s => string.Equals(s.Status, "TRADING", StringComparison.OrdinalIgnoreCase))
            .Where(s => s.Symbol is not null)
            .ToList();

        var currentSymbols = tradeable.Select(s => s.Symbol!).ToHashSet(StringComparer.OrdinalIgnoreCase);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // First poll: record the baseline, report nothing.
            if (_knownSymbols is null)
            {
                _knownSymbols = currentSymbols;
                return [];
            }

            var now = _timeProvider.GetUtcNow();
            var newListings = tradeable
                .Where(s => !_knownSymbols.Contains(s.Symbol!))
                .Select(s => new TokenListing
                {
                    Symbol = s.Symbol!,
                    BaseAsset = s.BaseAsset ?? string.Empty,
                    QuoteAsset = s.QuoteAsset ?? string.Empty,
                    Venue = "Binance",
                    DetectedUtc = now,
                })
                .ToList();

            _knownSymbols = currentSymbols;
            return newListings;
        }
        finally
        {
            _gate.Release();
        }
    }
}
