using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;
using Tremor.Core.Services.Detection;
using Tremor.Core.Services.Scanning;
using Xunit;

namespace Tremor.Core.Tests;

/// <summary>
/// Integration tests over the real <see cref="MarketScanner"/> with fake data
/// sources. These lock in the orchestration contract: detected events are
/// published to the alert sink, and notifications are delivered only when the
/// user has push enabled.
/// </summary>
public class MarketScannerTests
{
    private static readonly TokenListing SampleListing = new()
    {
        Symbol = "NEWUSDT",
        BaseAsset = "NEW",
        QuoteAsset = "USDT",
        Venue = "Binance",
        DetectedUtc = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public async Task PublishesDetectedListingAndNotifiesWhenPushEnabled()
    {
        var sink = new AlertSink();
        var notifier = new CapturingNotificationService();
        var scanner = BuildScanner(sink, notifier, pushEnabled: true);

        using var cts = new CancellationTokenSource();
        var run = scanner.RunAsync(cts.Token);

        var delivered = await notifier.WaitForFirstAsync(TimeSpan.FromSeconds(5));

        await StopAsync(cts, run);

        Assert.NotNull(delivered);
        Assert.Equal(AlertType.NewListing, delivered!.Type);
        Assert.Contains(sink.Recent, a => a.Type == AlertType.NewListing && a.Symbol == "NEWUSDT");
        Assert.Single(notifier.Delivered);
    }

    [Fact]
    public async Task PublishesToSinkButDoesNotNotifyWhenPushDisabled()
    {
        var sink = new AlertSink();
        var notifier = new CapturingNotificationService();

        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        sink.AlertPublished += (_, _) => published.TrySetResult();

        var scanner = BuildScanner(sink, notifier, pushEnabled: false);

        using var cts = new CancellationTokenSource();
        var run = scanner.RunAsync(cts.Token);

        var completed = await Task.WhenAny(published.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        await StopAsync(cts, run);

        Assert.Same(published.Task, completed); // the alert was published
        Assert.Contains(sink.Recent, a => a.Type == AlertType.NewListing);
        Assert.Empty(notifier.Delivered); // ...but nothing was delivered
    }

    private static async Task StopAsync(CancellationTokenSource cts, Task run)
    {
        cts.Cancel();
        try
        {
            await run;
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    private static MarketScanner BuildScanner(
        AlertSink sink,
        CapturingNotificationService notifier,
        bool pushEnabled)
    {
        var options = Options.Create(new TremorOptions { PollInterval = TimeSpan.FromMilliseconds(25) });

        return new MarketScanner(
            new IdleMarketDataService(),
            new SingleItemWatchlist(),
            new VolumeSpikeDetector(options),
            new OnceListingService(),
            new NoWhaleService(),
            sink,
            notifier,
            new FixedUserProfileService(pushEnabled),
            options,
            NullLogger<MarketScanner>.Instance);
    }

    // --- Fakes -------------------------------------------------------------

    private sealed class IdleMarketDataService : IMarketDataService
    {
        public Task<PriceTick?> GetTickerAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult<PriceTick?>(null);

        public Task<IReadOnlyList<PriceTick>> GetTickersAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PriceTick>>([]);

        public async IAsyncEnumerable<PriceTick> StreamTickersAsync(
            IReadOnlyCollection<string> symbols,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Block until cancelled so the volume loop idles rather than spins.
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            yield break;
        }
    }

    private sealed class SingleItemWatchlist : IWatchlistService
    {
        private readonly IReadOnlyList<WatchlistItem> _items =
        [
            new WatchlistItem
            {
                Token = new Token { Symbol = "BTCUSDT", BaseAsset = "BTC", QuoteAsset = "USDT" },
                AddedUtc = DateTimeOffset.UnixEpoch,
            },
        ];

        public event EventHandler? Changed { add { } remove { } }

        public Task<IReadOnlyList<WatchlistItem>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items);

        public Task<WatchlistItem> AddAsync(Token token, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RemoveAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> ContainsAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private sealed class OnceListingService : INewListingService
    {
        private int _calls;

        public Task<IReadOnlyList<TokenListing>> PollNewListingsAsync(CancellationToken cancellationToken = default)
        {
            var first = Interlocked.Increment(ref _calls) == 1;
            return Task.FromResult<IReadOnlyList<TokenListing>>(first ? [SampleListing] : []);
        }
    }

    private sealed class NoWhaleService : IWhaleAlertService
    {
        public Task<IReadOnlyList<WhaleAlert>> GetRecentAsync(IEnumerable<string> baseAssets, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WhaleAlert>>([]);
    }

    private sealed class FixedUserProfileService : IUserProfileService
    {
        private readonly UserProfile _profile;

        public FixedUserProfileService(bool pushEnabled)
            => _profile = new UserProfile { Id = "test", PushNotificationsEnabled = pushEnabled };

        public Task<UserProfile> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_profile);

        public Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CapturingNotificationService : INotificationService
    {
        private readonly List<Alert> _delivered = [];
        private readonly TaskCompletionSource<Alert> _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _lock = new();

        public IReadOnlyList<Alert> Delivered
        {
            get
            {
                lock (_lock)
                {
                    return _delivered.ToList();
                }
            }
        }

        public Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _delivered.Add(alert);
            }

            _first.TrySetResult(alert);
            return Task.CompletedTask;
        }

        public async Task<Alert?> WaitForFirstAsync(TimeSpan timeout)
        {
            var completed = await Task.WhenAny(_first.Task, Task.Delay(timeout)).ConfigureAwait(false);
            return completed == _first.Task ? _first.Task.Result : null;
        }
    }
}
