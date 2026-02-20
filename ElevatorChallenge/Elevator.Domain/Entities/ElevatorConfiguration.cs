using Elevator.Domain.Enums;

namespace Elevator.Domain.Entities;

/// <summary>
/// Defines the physical and operational configuration of a specific elevator
/// </summary>
public record ElevatorConfiguration
{
    public ElevatorType Type { get; init; } = ElevatorType.Local;
    
    /// <summary>
    /// Set of floors this elevator is allowed to service
    /// </summary>
    public IReadOnlySet<int> AllowedFloors { get; init; } = new HashSet<int>();
    
    /// <summary>
    /// Time taken to travel between adjacent floors
    /// </summary>
    public TimeSpan FloorTravelTime { get; init; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Time taken to complete a door open-wait-close cycle
    /// </summary>
    public TimeSpan DoorOperationTime { get; init; } = TimeSpan.FromSeconds(3);
}
