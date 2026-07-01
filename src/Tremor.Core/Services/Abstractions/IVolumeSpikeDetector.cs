using Tremor.Core.Models;

namespace Tremor.Core.Services.Abstractions;

/// <summary>
/// Detects when a symbol's volume moves materially above its recent baseline.
/// Stateful: it accumulates a rolling history per symbol as samples arrive.
/// </summary>
public interface IVolumeSpikeDetector
{
    /// <summary>
    /// Feed a new volume sample for a symbol. Returns a <see cref="VolumeSpikeAlert"/>
    /// if this sample qualifies as a spike against the accumulated baseline,
    /// otherwise <c>null</c>.
    /// </summary>
    VolumeSpikeAlert? Observe(string symbol, decimal volume, DateTimeOffset timestampUtc);

    /// <summary>Clear all accumulated history (e.g. on sign-out or reset).</summary>
    void Reset();
}
