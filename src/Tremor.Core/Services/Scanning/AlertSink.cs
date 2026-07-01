using System.Collections.Concurrent;
using Tremor.Core.Models;

namespace Tremor.Core.Services.Scanning;

/// <summary>
/// Thread-safe in-memory alert buffer. Keeps a bounded, de-duplicated history and
/// raises <see cref="AlertPublished"/> for live UI/notification consumers.
/// </summary>
public sealed class AlertSink : IAlertSink
{
    private const int MaxHistory = 200;

    private readonly object _lock = new();
    private readonly LinkedList<Alert> _recent = new();
    private readonly ConcurrentDictionary<string, byte> _seenIds = new();

    public event EventHandler<Alert>? AlertPublished;

    public void Publish(Alert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        // Ignore duplicates (same deterministic id) so repeated polls don't spam.
        if (!_seenIds.TryAdd(alert.Id, 0))
        {
            return;
        }

        lock (_lock)
        {
            _recent.AddFirst(alert);
            while (_recent.Count > MaxHistory)
            {
                var removed = _recent.Last!.Value;
                _recent.RemoveLast();
                _seenIds.TryRemove(removed.Id, out _);
            }
        }

        AlertPublished?.Invoke(this, alert);
    }

    public IReadOnlyList<Alert> Recent
    {
        get
        {
            lock (_lock)
            {
                return _recent.ToList();
            }
        }
    }
}
