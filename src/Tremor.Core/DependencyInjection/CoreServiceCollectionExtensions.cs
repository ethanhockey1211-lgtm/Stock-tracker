using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tremor.Core.Configuration;
using Tremor.Core.Services.Abstractions;
using Tremor.Core.Services.Affiliate;
using Tremor.Core.Services.Detection;
using Tremor.Core.Services.Listings;
using Tremor.Core.Services.Market;
using Tremor.Core.Services.Persistence;
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

        // Local persistence. Default is in-memory so Core works standalone and in
        // tests; the platform head replaces IPreferencesStore with a file-backed one
        // via AddFilePersistence(path) so data survives app restarts.
        services.TryAddSingleton<IPreferencesStore, InMemoryPreferencesStore>();

        // Detection + state.
        services.AddSingleton<IVolumeSpikeDetector, VolumeSpikeDetector>();
        services.AddSingleton<IWhaleAlertService, PlaceholderWhaleAlertService>();
        services.AddSingleton<IWatchlistService, PersistentWatchlistService>();
        services.AddSingleton<IUserProfileService, PersistentUserProfileService>();
        services.AddSingleton<IAffiliateLinkService, AffiliateLinkService>();
        services.AddSingleton<IAlertSink, AlertSink>();

        // Orchestrator. NOTE: the concrete INotificationService must be registered
        // by the platform head before MarketScanner is resolved.
        services.AddSingleton<MarketScanner>();

        return services;
    }

    /// <summary>
    /// Replace the default in-memory preferences store with a file-backed one so
    /// the watchlist and user profile persist across restarts. Call after
    /// <see cref="AddTremorCore"/> — the later registration wins on resolution.
    /// </summary>
    public static IServiceCollection AddFilePersistence(this IServiceCollection services, string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        services.AddSingleton<IPreferencesStore>(_ => new FilePreferencesStore(directoryPath));
        return services;
    }
}
