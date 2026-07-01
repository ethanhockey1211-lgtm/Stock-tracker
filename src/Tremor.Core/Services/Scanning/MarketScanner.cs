using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Scanning;

/// <summary>
/// Background orchestrator that watches the market and publishes detected events.
/// It runs three loops for the tokens on the user's watchlist:
///   1. live price/volume stream  -> volume-spike detection
///   2. periodic new-listing poll -> new-listing alerts
///   3. periodic whale poll        -> whale-movement alerts
///
/// Every published alert is also handed to <see cref="INotificationService"/> for
/// push/local delivery. Detection describes what has already happened; it never
/// forecasts. This runs on-device in the MVP; the same logic can move to a
/// backend later behind the same service interfaces.
/// </summary>
public sealed class MarketScanner
{
    private readonly IMarketDataService _marketData;
    private readonly IWatchlistService _watchlist;
    private readonly IVolumeSpikeDetector _volumeDetector;
    private readonly INewListingService _listings;
    private readonly IWhaleAlertService _whales;
    private readonly IAlertSink _alertSink;
    private readonly INotificationService _notifications;
    private readonly IUserProfileService _userProfiles;
    private readonly TremorOptions _options;
    private readonly ILogger<MarketScanner> _logger;

    public MarketScanner(
        IMarketDataService marketData,
        IWatchlistService watchlist,
        IVolumeSpikeDetector volumeDetector,
        INewListingService listings,
        IWhaleAlertService whales,
        IAlertSink alertSink,
        INotificationService notifications,
        IUserProfileService userProfiles,
        IOptions<TremorOptions> options,
        ILogger<MarketScanner> logger)
    {
        _marketData = marketData;
        _watchlist = watchlist;
        _volumeDetector = volumeDetector;
        _listings = listings;
        _whales = whales;
        _alertSink = alertSink;
        _notifications = notifications;
        _userProfiles = userProfiles;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Run all scan loops until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Market scanner starting with poll interval {PollInterval} and volume spike thresholds {BaselineWindow}/{MinSamples}/{SpikeMultiple}",
            _options.PollInterval,
            _options.VolumeSpike.BaselineWindow,
            _options.VolumeSpike.MinSamples,
            _options.VolumeSpike.SpikeMultiple);

        var tasks = new[]
        {
            RunVolumeStreamAsync(cancellationToken),
            RunListingPollAsync(cancellationToken),
            RunWhalePollAsync(cancellationToken),
        };

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            _logger.LogInformation("Market scanner stopped");
        }
    }

    private async Task RunVolumeStreamAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var items = await _watchlist.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var symbols = items.Select(i => i.Symbol).ToArray();
            _logger.LogDebug("Volume stream loop starting with {SymbolCount} watched symbols", symbols.Length);

            if (symbols.Length == 0)
            {
                _logger.LogDebug("No symbols on watchlist; sleeping for {Delay}", _options.PollInterval);
                await DelayQuietlyAsync(_options.PollInterval, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                _logger.LogDebug("Opening market data stream for symbols {Symbols}", string.Join(", ", symbols));
                await foreach (var tick in _marketData
                    .StreamTickersAsync(symbols, cancellationToken)
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    _logger.LogTrace("Observed tick for {Symbol} at {TimestampUtc} with quote volume {QuoteVolume24h}", tick.Symbol, tick.TimestampUtc, tick.QuoteVolume24h);
                    var spike = _volumeDetector.Observe(tick.Symbol, tick.QuoteVolume24h, tick.TimestampUtc);
                    if (spike is not null)
                    {
                        _logger.LogInformation("Detected volume spike for {Symbol} at {DetectedUtc}", spike.Symbol, spike.DetectedUtc);
                        await PublishAsync(spike, cancellationToken).ConfigureAwait(false);
                    }

                    // Rebuild the stream if the watchlist changed underneath us.
                    if (await WatchlistChangedAsync(symbols, cancellationToken).ConfigureAwait(false))
                    {
                        _logger.LogDebug("Watchlist changed; reconnecting volume stream");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Volume stream error; reconnecting for symbols {Symbols}", string.Join(", ", symbols));
                await DelayQuietlyAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task RunListingPollAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var listings = await _listings.PollNewListingsAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Listing poll returned {ListingCount} results", listings.Count);
                foreach (var listing in listings)
                {
                    var alert = new Alert
                    {
                        Id = $"listing:{listing.Venue}:{listing.Symbol}",
                        Type = AlertType.NewListing,
                        Severity = AlertSeverity.Notable,
                        Symbol = listing.Symbol,
                        Title = Copy.AppCopy.NewListingHeadline,
                        Detail = $"{listing.Pair} is now trading on {listing.Venue}.",
                        DetectedUtc = listing.DetectedUtc,
                    };
                    _logger.LogInformation("Publishing new-listing alert for {Symbol} from {Venue}", listing.Symbol, listing.Venue);
                    await PublishAsync(alert, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Listing poll error");
            }

            await DelayQuietlyAsync(_options.PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunWhalePollAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var items = await _watchlist.GetAllAsync(cancellationToken).ConfigureAwait(false);
                var assets = items.Select(i => i.Token.BaseAsset).Distinct().ToArray();
                _logger.LogDebug("Whale poll starting with {AssetCount} watched assets", assets.Length);

                if (assets.Length > 0)
                {
                    var whales = await _whales.GetRecentAsync(assets, cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Whale poll returned {WhaleCount} results", whales.Count);
                    foreach (var whale in whales)
                    {
                        _logger.LogInformation("Publishing whale alert for {Symbol}", whale.Symbol);
                        await PublishAsync(whale, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Whale poll error");
            }

            await DelayQuietlyAsync(_options.PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PublishAsync(Alert alert, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Publishing alert {AlertId} [{AlertType}] for {Symbol}", alert.Id, alert.Type, alert.Symbol ?? "<none>");
        _alertSink.Publish(alert);

        var profile = await _userProfiles.GetAsync(cancellationToken).ConfigureAwait(false);
        if (profile.PushNotificationsEnabled)
        {
            _logger.LogDebug("Dispatching notification for alert {AlertId}", alert.Id);
            try
            {
                await _notifications.NotifyAsync(alert, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deliver notification for alert {AlertId}", alert.Id);
            }
        }
        else
        {
            _logger.LogDebug("Notifications disabled for profile; skipping delivery for {AlertId}", alert.Id);
        }
    }

    private async Task<bool> WatchlistChangedAsync(string[] streaming, CancellationToken cancellationToken)
    {
        var items = await _watchlist.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var current = items.Select(i => i.Symbol).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return !current.SetEquals(streaming);
    }

    private static async Task DelayQuietlyAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignored — caller loop checks the token.
        }
    }
}
