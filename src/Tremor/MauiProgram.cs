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

        // Platform-agnostic services (market data, detection, watchlist, etc.).
        builder.Services.AddTremorCore();

        // Platform notification delivery. Swap for a Firebase Cloud Messaging-backed
        // implementation when push is wired up; MarketScanner only depends on the
        // INotificationService abstraction.
        builder.Services.AddSingleton<INotificationService, MauiLocalNotificationService>();

        // View models.
        builder.Services.AddSingleton<WatchlistViewModel>();
        builder.Services.AddSingleton<AlertsViewModel>();
        builder.Services.AddSingleton<ListingsViewModel>();
        builder.Services.AddSingleton<SettingsViewModel>();

        // Pages.
        builder.Services.AddSingleton<WatchlistPage>();
        builder.Services.AddSingleton<AlertsPage>();
        builder.Services.AddSingleton<ListingsPage>();
        builder.Services.AddSingleton<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
