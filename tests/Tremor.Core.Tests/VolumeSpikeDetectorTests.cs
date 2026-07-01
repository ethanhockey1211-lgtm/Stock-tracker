using Microsoft.Extensions.Options;
using Tremor.Core.Configuration;
using Tremor.Core.Models;
using Tremor.Core.Services.Detection;
using Xunit;

namespace Tremor.Core.Tests;

public class VolumeSpikeDetectorTests
{
    private static VolumeSpikeDetector CreateDetector(VolumeSpikeOptions? opts = null)
    {
        var options = new TremorOptions { VolumeSpike = opts ?? new VolumeSpikeOptions() };
        return new VolumeSpikeDetector(Options.Create(options));
    }

    private static DateTimeOffset At(int seconds) => DateTimeOffset.UnixEpoch.AddSeconds(seconds);

    [Fact]
    public void DoesNotReportBeforeMinimumSamples()
    {
        var detector = CreateDetector(new VolumeSpikeOptions { MinSamples = 5, SpikeMultiple = 3m });

        // Feed a huge value while below MinSamples: no alert yet.
        for (var i = 0; i < 4; i++)
        {
            Assert.Null(detector.Observe("BTCUSDT", 1_000_000m, At(i)));
        }
    }

    [Fact]
    public void ReportsSpikeAboveMultiple()
    {
        var detector = CreateDetector(new VolumeSpikeOptions
        {
            MinSamples = 5,
            BaselineWindow = 20,
            SpikeMultiple = 3m,
        });

        // Establish a baseline of ~100.
        for (var i = 0; i < 5; i++)
        {
            Assert.Null(detector.Observe("BTCUSDT", 100m, At(i)));
        }

        // 400 is 4x the baseline of 100 -> spike.
        var alert = detector.Observe("BTCUSDT", 400m, At(10));

        Assert.NotNull(alert);
        Assert.Equal(AlertType.VolumeSpike, alert!.Type);
        Assert.Equal("BTCUSDT", alert.Symbol);
        Assert.Equal(100m, alert.BaselineVolume);
        Assert.Equal(400m, alert.ObservedVolume);
        Assert.Equal(4m, alert.Multiple);
    }

    [Fact]
    public void DoesNotReportBelowMultiple()
    {
        var detector = CreateDetector(new VolumeSpikeOptions { MinSamples = 3, SpikeMultiple = 3m });

        for (var i = 0; i < 3; i++)
        {
            detector.Observe("ETHUSDT", 100m, At(i));
        }

        // 2.5x is under the 3x threshold.
        Assert.Null(detector.Observe("ETHUSDT", 250m, At(10)));
    }

    [Fact]
    public void FlagsHighSeverityAboveHighMultiple()
    {
        var detector = CreateDetector(new VolumeSpikeOptions
        {
            MinSamples = 3,
            SpikeMultiple = 3m,
            HighSeverityMultiple = 6m,
        });

        for (var i = 0; i < 3; i++)
        {
            detector.Observe("SOLUSDT", 100m, At(i));
        }

        var alert = detector.Observe("SOLUSDT", 700m, At(10)); // 7x
        Assert.NotNull(alert);
        Assert.Equal(AlertSeverity.High, alert!.Severity);
    }

    [Fact]
    public void TracksSymbolsIndependently()
    {
        var detector = CreateDetector(new VolumeSpikeOptions { MinSamples = 2, SpikeMultiple = 3m });

        detector.Observe("AAAUSDT", 100m, At(0));
        detector.Observe("AAAUSDT", 100m, At(1));

        // BBB has no history yet, so a big value must not alert.
        Assert.Null(detector.Observe("BBBUSDT", 999m, At(2)));

        // AAA has a baseline of 100, so 500 alerts.
        Assert.NotNull(detector.Observe("AAAUSDT", 500m, At(3)));
    }

    [Fact]
    public void IgnoresNegativeVolume()
    {
        var detector = CreateDetector(new VolumeSpikeOptions { MinSamples = 2, SpikeMultiple = 3m });
        Assert.Null(detector.Observe("BTCUSDT", -5m, At(0)));
    }

    [Fact]
    public void ResetClearsHistory()
    {
        var detector = CreateDetector(new VolumeSpikeOptions { MinSamples = 2, SpikeMultiple = 3m });

        detector.Observe("BTCUSDT", 100m, At(0));
        detector.Observe("BTCUSDT", 100m, At(1));
        detector.Reset();

        // After reset the baseline is gone, so a large value can't alert yet.
        Assert.Null(detector.Observe("BTCUSDT", 500m, At(2)));
    }
}
