using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tremor.Core.Configuration;
using Tremor.Core.Services.Abstractions;
using Tremor.Core.Services.Affiliate;
using Tremor.Core.Services.Detection;
using Tremor.Core.Services.Listings;
using Tremor.Core.Services.Market;
using Tremor.Core.Services.Scanning;
using Tremor.Core.Services.Users;
using Tremor.Core.Services.Watchlist;
using Tremor.Core.Services.Whales;

namespace Tremor.Core.DependencyInjection;

/// <summary>
/// Registers Tremor's platform-agnostic services. The MAUI head calls this and
/// then adds its own platform pieces (e.g. the concrete
/// <see cref="INotificationService"/> backed by Firebase Cloud Messaging).
/// </summary>
public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddTremorCore(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<TremorOptions>(configuration.GetSection(TremorOptions.SectionName));
        }
        else
        {
            services.AddOptions<TremorOptions>();
        }

        services.AddSingleton(TimeProvider.System);

        // Market data + listing discovery use typed HttpClients.
        services.AddHttpClient<IMarketDataService, BinanceMarketDataService>();
        services.AddHttpClient<INewListingService, BinanceNewListingService>();

        // Detection + state.
        services.AddSingleton<IVolumeSpikeDetector, VolumeSpikeDetector>();
        services.AddSingleton<IWhaleAlertService, PlaceholderWhaleAlertService>();
        services.AddSingleton<IWatchlistService, InMemoryWatchlistService>();
        services.AddSingleton<IUserProfileService, InMemoryUserProfileService>();
        services.AddSingleton<IAffiliateLinkService, AffiliateLinkService>();
        services.AddSingleton<IAlertSink, AlertSink>();

        // Orchestrator. NOTE: the concrete INotificationService must be registered
        // by the platform head before MarketScanner is resolved.
        services.AddSingleton<MarketScanner>();

        return services;
    }
}
