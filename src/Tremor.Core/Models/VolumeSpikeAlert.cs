namespace Tremor.Core.Models;

/// <summary>
/// A volume observation that sits materially above a recent baseline.
/// Describes what the volume did — not what price will do next.
/// </summary>
public sealed class VolumeSpikeAlert : Alert
{
    public VolumeSpikeAlert()
    {
        Type = AlertType.VolumeSpike;
    }

    /// <summary>The volume value that triggered the spike.</summary>
    public decimal ObservedVolume { get; init; }

    /// <summary>The baseline (e.g. rolling average) the observation is compared against.</summary>
    public decimal BaselineVolume { get; init; }

    /// <summary>ObservedVolume / BaselineVolume. A ratio of 3.0 means 3x the baseline.</summary>
    public decimal Multiple { get; init; }
}
