using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Copy;
using Tremor.Core.Models;
using Tremor.Core.Services.Abstractions;

namespace Tremor.Core.Services.Detection;

/// <summary>
/// Rolling-baseline volume-spike detector.
///
/// For each symbol it keeps the last <see cref="VolumeSpikeOptions.BaselineWindow"/>
/// samples. When a new sample arrives, it compares it against the mean of the
/// samples seen <em>before</em> it. If the new sample is at least
/// <see cref="VolumeSpikeOptions.SpikeMultiple"/> times that baseline (and enough
/// samples exist), it reports a spike. The new sample is then folded into the
/// window regardless.
///
/// This describes what volume <em>did</em> — it is not a prediction.
/// </summary>
public sealed class VolumeSpikeDetector : IVolumeSpikeDetector
{
    private readonly VolumeSpikeOptions _options;
    private readonly ConcurrentDictionary<string, Queue<decimal>> _history = new(StringComparer.OrdinalIgnoreCase);

    public VolumeSpikeDetector(IOptions<TremorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value.VolumeSpike;
    }

    public VolumeSpikeAlert? Observe(string symbol, decimal volume, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        // Negative volume is nonsensical; ignore it rather than skew the baseline.
        if (volume < 0)
        {
            return null;
        }

        var window = _history.GetOrAdd(symbol, static _ => new Queue<decimal>());

        VolumeSpikeAlert? alert = null;

        lock (window)
        {
            if (window.Count >= _options.MinSamples)
            {
                var baseline = Average(window);

                // Only meaningful when there is a positive baseline to compare against.
                if (baseline > 0)
                {
                    var multiple = volume / baseline;
                    if (multiple >= _options.SpikeMultiple)
                    {
                        alert = BuildAlert(symbol, volume, baseline, multiple, timestampUtc);
                    }
                }
            }

            window.Enqueue(volume);
            while (window.Count > _options.BaselineWindow)
            {
                window.Dequeue();
            }
        }

        return alert;
    }

    public void Reset() => _history.Clear();

    private VolumeSpikeAlert BuildAlert(
        string symbol,
        decimal volume,
        decimal baseline,
        decimal multiple,
        DateTimeOffset timestampUtc)
    {
        var severity = multiple >= _options.HighSeverityMultiple
            ? AlertSeverity.High
            : AlertSeverity.Notable;

        var roundedMultiple = Math.Round(multiple, 1);

        return new VolumeSpikeAlert
        {
            Id = $"vol:{symbol}:{timestampUtc.ToUnixTimeMilliseconds()}",
            Symbol = symbol,
            Severity = severity,
            Title = AppCopy.VolumeSpikeHeadline,
            Detail = $"{symbol} traded {roundedMultiple}x its recent average volume.",
            DetectedUtc = timestampUtc,
            ObservedVolume = volume,
            BaselineVolume = baseline,
            Multiple = multiple,
        };
    }

    private static decimal Average(Queue<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        decimal sum = 0m;
        foreach (var v in values)
        {
            sum += v;
        }

        return sum / values.Count;
    }
}
