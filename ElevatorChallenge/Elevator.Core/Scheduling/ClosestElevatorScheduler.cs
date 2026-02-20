using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Scheduling;

/// <summary>
/// Scheduler that prioritizes elevators closest to the request pickup floor
/// that are either idle or already moving in the direction of the request.
/// </summary>
public class ClosestElevatorScheduler : IScheduler
{
    public IElevator? SelectElevator(Request request, IEnumerable<IElevator> availableElevators)
    {
        var elevators = availableElevators.ToList();
        
        if (!elevators.Any())
            return null;

        return elevators
            .Where(e => CanService(e, request))
            .OrderBy(e => CalculateDistance(e, request))
            .ThenBy(e => e.PendingCount)
            .FirstOrDefault();
    }

    private bool CanService(IElevator elevator, Request request)
    {
        if (elevator.State == ElevatorState.Idle)
            return true;
            
        // Check if moving towards the pickup floor
        if (elevator.State == ElevatorState.MovingUp && request.PickupFloor >= elevator.CurrentFloor)
            return true;
            
        if (elevator.State == ElevatorState.MovingDown && request.PickupFloor <= elevator.CurrentFloor)
            return true;
            
        return false;
    }

    private int CalculateDistance(IElevator elevator, Request request)
    {
        return Math.Abs(elevator.CurrentFloor - request.PickupFloor);
    }
}
