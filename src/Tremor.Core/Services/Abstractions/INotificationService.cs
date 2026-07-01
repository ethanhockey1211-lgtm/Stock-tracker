using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Delivers a detected event to the user as a push/local notification.
/// The concrete implementation lives in the MAUI head (Firebase Cloud Messaging
/// for real push; local notifications as a fallback). Core only depends on this
/// abstraction so detection logic stays platform-agnostic and testable.
/// </summary>
public interface INotificationService
{
    /// <summary>Show/deliver a notification describing a detected alert.</summary>
    Task NotifyAsync(Alert alert, CancellationToken cancellationToken = default);
}
