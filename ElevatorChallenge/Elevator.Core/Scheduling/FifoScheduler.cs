using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Scheduling;

/// <summary>
/// Basic scheduler that simply assigns to the first available/idle elevator
/// or the one with the fewest pending requests.
/// </summary>
public class FifoScheduler : IScheduler
{
    public IElevator? SelectElevator(Request request, IEnumerable<IElevator> availableElevators)
    {
        var elevators = availableElevators.ToList();
        
        if (!elevators.Any())
            return null;

        // Try to find an idle elevator first
        var idleElevator = elevators.FirstOrDefault(e => e.State == ElevatorState.Idle);
        if (idleElevator != null)
        {
            return idleElevator;
        }

        // If all are busy, assign to the one with the least pending requests
        return elevators.OrderBy(e => e.PendingCount).First();
    }
}
