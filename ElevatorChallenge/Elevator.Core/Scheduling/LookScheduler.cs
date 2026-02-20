using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Scheduling;

/// <summary>
/// Scheduler simulating the LOOK algorithm, an optimized version of SCAN
/// that reverses direction when there are no more requests in current direction.
/// </summary>
public class LookScheduler : IScheduler
{
    public IElevator? SelectElevator(Request request, IEnumerable<IElevator> availableElevators)
    {
        var elevators = availableElevators.ToList();
        
        if (!elevators.Any())
            return null;

        // The logic is structurally similar to SCAN in the assignment phase,
        // but the elevator's internal processing loop (Elevator.cs) handles the LOOK "reversal"
        // by looking at its remaining queue. For dispatching, we find the best fit on the path.
        var bestFit = elevators
            .Where(e => e.State == ElevatorState.Idle || 
                       (e.State == ElevatorState.MovingUp && request.PickupFloor >= e.CurrentFloor) ||
                       (e.State == ElevatorState.MovingDown && request.PickupFloor <= e.CurrentFloor))
            .OrderBy(e => Math.Abs(e.CurrentFloor - request.PickupFloor))
            .FirstOrDefault();

        return bestFit ?? elevators.OrderBy(e => e.PendingCount).First();
    }
}
