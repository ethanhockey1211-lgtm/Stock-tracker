using System.Diagnostics;

namespace Tremor.Core.Models;

/// <summary>
/// The category of a detected market event.
/// </summary>
public enum AlertType
{
    VolumeSpike,
    WhaleMovement,
    NewListing,
}

/// <summary>
/// Severity is a description of how far an observation sits outside its baseline —
/// it is not a signal to act. The user decides what, if anything, to do.
/// </summary>
public enum AlertSeverity
{
    Info,
    Notable,
    High,
}

/// <summary>
/// A detected, already-happened market event surfaced to the user.
/// Alerts describe observations only. They never contain predictions,
/// price targets, or buy/sell recommendations.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Alert
{
    public required string Id { get; init; }

    /// <summary>
    /// Alert category. Concrete subclasses set this in their constructor; when
    /// creating a plain <see cref="Alert"/> directly, set it in the initializer.
    /// </summary>
    public AlertType Type { get; init; }

    public AlertSeverity Severity { get; init; } = AlertSeverity.Info;

    /// <summary>Related trading symbol, when applicable (e.g. "BTCUSDT").</summary>
    public string? Symbol { get; init; }

    /// <summary>Short, factual, past/present-tense headline of what was detected.</summary>
    public required string Title { get; init; }

    /// <summary>Factual detail describing the observation.</summary>
    public required string Detail { get; init; }

    /// <summary>When the underlying event was detected (UTC).</summary>
    public DateTimeOffset DetectedUtc { get; init; }

    private string DebuggerDisplay => $"{Type} {Severity} {Symbol ?? "<none>"}: {Title}";
}
