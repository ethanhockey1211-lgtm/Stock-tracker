using Tremor.Core.Models;
using Tremor.Core.Services.Scanning;
using Xunit;

namespace Tremor.Core.Tests;

public class AlertSinkTests
{
    private static Alert MakeAlert(string id) => new()
    {
        Id = id,
        Type = AlertType.VolumeSpike,
        Title = "Volume spike detected",
        Detail = "test",
        DetectedUtc = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public void PublishAddsToRecentNewestFirst()
    {
        var sink = new AlertSink();
        sink.Publish(MakeAlert("a"));
        sink.Publish(MakeAlert("b"));

        Assert.Equal(2, sink.Recent.Count);
        Assert.Equal("b", sink.Recent[0].Id);
        Assert.Equal("a", sink.Recent[1].Id);
    }

    [Fact]
    public void DuplicateIdsAreIgnored()
    {
        var sink = new AlertSink();
        sink.Publish(MakeAlert("dup"));
        sink.Publish(MakeAlert("dup"));

        Assert.Single(sink.Recent);
    }

    [Fact]
    public void RaisesEventOnlyForNewAlerts()
    {
        var sink = new AlertSink();
        var fired = 0;
        sink.AlertPublished += (_, _) => fired++;

        sink.Publish(MakeAlert("x"));
        sink.Publish(MakeAlert("x")); // duplicate, should not fire

        Assert.Equal(1, fired);
    }
}
