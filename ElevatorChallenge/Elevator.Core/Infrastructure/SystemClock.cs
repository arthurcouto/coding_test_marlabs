using Elevator.Domain.Interfaces;

namespace Elevator.Core.Infrastructure;

/// <summary>
/// Production implementation of IClock
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public Task Delay(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
