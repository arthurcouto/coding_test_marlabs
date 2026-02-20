using Elevator.Domain.Events;

namespace Elevator.Domain.Interfaces;

/// <summary>
/// Observes systemic events and aggregates telemetry data
/// </summary>
public interface IMetricsCollector
{
    void RecordWaitTime(TimeSpan waitTime);
    void RecordTripTime(TimeSpan tripTime);
    void RecordElevatorUtilization(Guid elevatorId, int count);
    
    // Subscribe these to system events
    void OnRequestCompleted(object? sender, RequestCompletedEventArgs e);
    void OnStateChanged(object? sender, StateChangedEventArgs e);
}
