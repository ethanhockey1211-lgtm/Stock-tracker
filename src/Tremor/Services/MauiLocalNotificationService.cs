using Microsoft.Extensions.Logging;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Services;

/// <summary>
/// MVP notification delivery.
///
/// This scaffold logs each alert and is the single seam where real delivery gets
/// wired in. Two paths are expected (see project brief, feature 5):
///   - Firebase Cloud Messaging for remote push (covers iOS + Android) once a
///     backend can send messages, and
///   - a local-notification plugin (e.g. Plugin.LocalNotification) for on-device
///     alerts raised by the in-app <c>MarketScanner</c>.
///
/// Both satisfy <see cref="INotificationService"/>, so nothing else in the app
/// changes when they land.
/// </summary>
public sealed class MauiLocalNotificationService : INotificationService
{
    private readonly ILogger<MauiLocalNotificationService> _logger;

    public MauiLocalNotificationService(ILogger<MauiLocalNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        // Factual, past/present-tense payload only — never a prediction or a
        // buy/sell prompt (title/detail originate from AppCopy-controlled text).
        _logger.LogInformation(
            "Notification: [{Severity}] {Title} — {Detail}",
            alert.Severity,
            alert.Title,
            alert.Detail);

        // TODO: raise a platform notification here (FCM / local-notification plugin).
        return Task.CompletedTask;
    }
}
