using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Scheduling;

/// <summary>
/// Scheduler simulating the SCAN (Elevator) algorithm for disk drives.
/// Elevators sweep from one end to the other processing requests along the way.
/// </summary>
public class ScanScheduler : IScheduler
{
    public IElevator? SelectElevator(Request request, IEnumerable<IElevator> availableElevators)
    {
        var elevators = availableElevators.ToList();
        
        if (!elevators.Any())
            return null;

        // Find elevators that are moving in the requested direction
        // and haven't passed the pickup floor yet.
        var suitableElevator = elevators.FirstOrDefault(e => 
            (e.State == ElevatorState.MovingUp && request.Direction == Direction.Up && request.PickupFloor >= e.CurrentFloor) ||
            (e.State == ElevatorState.MovingDown && request.Direction == Direction.Down && request.PickupFloor <= e.CurrentFloor) ||
            e.State == ElevatorState.Idle
        );

        // If none found in ideal state, fallback to least busy
        return suitableElevator ?? elevators.OrderBy(e => e.PendingCount).First();
    }
}
