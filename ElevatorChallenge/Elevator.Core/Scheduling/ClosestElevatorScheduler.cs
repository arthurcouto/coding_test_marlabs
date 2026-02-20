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
            .OrderBy(e => CalculateScore(e, request))
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

    private int CalculateScore(IElevator elevator, Request request)
    {
        // Base score is distance (lower is better)
        int score = Math.Abs(elevator.CurrentFloor - request.PickupFloor);
        
        // Tie-breaker by queue size
        score += (elevator.PendingCount * 10);
        
        // Massive bonus for Express elevators if they reached this far (since Dispatcher already checked AllowedFloors)
        // This ensures an Express at floor 10 wins an allowed ride from 1 to 20 over a Local at floor 2
        if (elevator.Configuration.Type == ElevatorType.Express)
        {
            score -= 10000;
        }

        // VIP requests want the emptiest elevator possible, massively penalized for having a queue
        if (request.IsVip && elevator.PendingCount > 0)
        {
            score += 50000;
        }

        return score;
    }
}
