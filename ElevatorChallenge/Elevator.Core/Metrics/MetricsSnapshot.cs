namespace Elevator.Core.Metrics;

/// <summary>
/// Point-in-time representation of system metrics
/// </summary>
public record MetricsSnapshot(
    TimeSpan AverageWaitTime,
    TimeSpan AverageTripTime,
    int RequestsCompleted,
    IReadOnlyDictionary<Guid, int> ElevatorUtilization
);
