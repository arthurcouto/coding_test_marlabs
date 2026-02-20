namespace Elevator.Domain.Interfaces;

/// <summary>
/// Abstraction for time to allow deterministic testing
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
    Task Delay(TimeSpan delay, CancellationToken cancellationToken);
}
