using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tremor.Core.DependencyInjection;
using Tremor.Core.Services.Abstractions;
using Tremor.Services;
using Tremor.ViewModels;
using Tremor.Views;

namespace Tremor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // System fonts are used by default. Register bundled fonts here
                // once TTF files are added under Resources/Fonts.
            });

        // Optional bundled configuration (thresholds, affiliate link, etc.).
        // Falls back to TremorOptions defaults if the file is missing/unreadable.
        LoadBundledAppSettings(builder);

        // Platform-agnostic services (market data, detection, watchlist, etc.).
        builder.Services.AddTremorCore(builder.Configuration);

        // Persist watchlist + profile to the app data directory across restarts.
        builder.Services.AddFilePersistence(FileSystem.AppDataDirectory);

        // Platform notification delivery. Swap for a Firebase Cloud Messaging-backed
        // implementation when push is wired up; MarketScanner only depends on the
        // INotificationService abstraction.
        builder.Services.AddSingleton<INotificationService, MauiLocalNotificationService>();

        // View models.
        builder.Services.AddSingleton<WatchlistViewModel>();
        builder.Services.AddSingleton<AlertsViewModel>();
        builder.Services.AddSingleton<ListingsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddTransient<TokenDetailViewModel>();

        // Pages.
        builder.Services.AddSingleton<WatchlistPage>();
        builder.Services.AddSingleton<AlertsPage>();
        builder.Services.AddSingleton<ListingsPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddTransient<TokenDetailPage>();

#if DEBUG
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        return builder.Build();
    }

    private static void LoadBundledAppSettings(MauiAppBuilder builder)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json")
                .GetAwaiter()
                .GetResult();
            builder.Configuration.AddJsonStream(stream);
        }
        catch (Exception)
        {
            // No bundled config (or unreadable): defaults in TremorOptions apply.
        }
    }
}
