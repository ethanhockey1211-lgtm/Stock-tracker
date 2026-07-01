using Tremor.Core.Models;

namespace Tremor.Core.Services.Scanning;

/// <summary>
/// Receives alerts produced by the scanner and makes them available to the UI.
/// Kept separate from delivery (notifications) so the two can evolve independently.
/// </summary>
public interface IAlertSink
{
    /// <summary>Raised whenever a new alert is published.</summary>
    event EventHandler<Alert>? AlertPublished;

    /// <summary>Publish a newly detected alert.</summary>
    void Publish(Alert alert);

    /// <summary>Most recent alerts, newest first.</summary>
    IReadOnlyList<Alert> Recent { get; }
}
