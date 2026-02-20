using System.Collections.Concurrent;
using Elevator.Domain.Events;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Metrics;

/// <summary>
/// In-memory aggregation of system telemetry
/// </summary>
public class InMemoryMetricsCollector : IMetricsCollector
{
    private readonly ConcurrentBag<TimeSpan> _waitTimes = new();
    private readonly ConcurrentBag<TimeSpan> _tripTimes = new();
    private readonly ConcurrentDictionary<Guid, int> _utilization = new();
    private int _requestsCompleted = 0;

    public void RecordWaitTime(TimeSpan waitTime)
    {
        _waitTimes.Add(waitTime);
    }

    public void RecordTripTime(TimeSpan tripTime)
    {
        _tripTimes.Add(tripTime);
    }

    public void RecordElevatorUtilization(Guid elevatorId, int count)
    {
        _utilization.AddOrUpdate(elevatorId, count, (_, prev) => prev + count);
    }

    public void OnRequestCompleted(object? sender, RequestCompletedEventArgs e)
    {
        Interlocked.Increment(ref _requestsCompleted);
        RecordWaitTime(e.Duration); // Simplified: treating total duration as wait time for this example
        RecordElevatorUtilization(e.ElevatorId, 1);
    }

    public void OnStateChanged(object? sender, StateChangedEventArgs e)
    {
        // Example: Track time spent in DoorOpening state
    }

    public MetricsSnapshot GetSnapshot()
    {
        var waitAvg = _waitTimes.Any() ? TimeSpan.FromTicks((long)_waitTimes.Average(ts => ts.Ticks)) : TimeSpan.Zero;
        var tripAvg = _tripTimes.Any() ? TimeSpan.FromTicks((long)_tripTimes.Average(ts => ts.Ticks)) : TimeSpan.Zero;

        return new MetricsSnapshot(
            AverageWaitTime: waitAvg,
            AverageTripTime: tripAvg,
            RequestsCompleted: _requestsCompleted,
            ElevatorUtilization: _utilization.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        );
    }
}
