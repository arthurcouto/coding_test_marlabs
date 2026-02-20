namespace Elevator.Domain.Enums;

/// <summary>
/// Defines the type of elevator in the system
/// </summary>
public enum ElevatorType
{
    Local,      // Stops at all floors
    Express,    // Stops only at specific floors (e.g., ground and high floors)
    Freight     // For cargo, may have different speed or ignore passenger requests
}
