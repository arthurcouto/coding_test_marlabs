using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Elevators;

/// <summary>
/// Specific configuration for Freight Elevators
/// May prioritize VIP/Cargo requests or operate slower
/// </summary>
public class FreightElevator : Elevator
{
    public FreightElevator(ElevatorConfiguration configuration, IClock clock) 
        : base(configuration, clock)
    {
        if (configuration.Type != ElevatorType.Freight)
        {
            throw new ArgumentException("Configuration type must be Freight");
        }
    }

    protected override async Task ProcessRequestAsync(Request request, CancellationToken cancellationToken)
    {
        // Freight elevators might have specific rules, like slower doors
        // For now, it reuses the base processing logic
        await base.ProcessRequestAsync(request, cancellationToken);
    }
}
