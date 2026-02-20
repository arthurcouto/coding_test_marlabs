using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Elevators;

/// <summary>
/// Specific configuration for Express Elevators
/// Usually skips lower floors to service higher floors faster
/// </summary>
public class ExpressElevator : Elevator
{
    public ExpressElevator(ElevatorConfiguration configuration, IClock clock) 
        : base(configuration, clock)
    {
        if (configuration.Type != ElevatorType.Express)
        {
            throw new ArgumentException("Configuration type must be Express");
        }
    }
}
