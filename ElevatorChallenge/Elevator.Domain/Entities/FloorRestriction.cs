namespace Elevator.Domain.Entities;

/// <summary>
/// Represents access restrictions for specific floors
/// </summary>
public record FloorRestriction
{
    public int FloorNumber { get; init; }
    
    /// <summary>
    /// If true, only VIP requests or specific elevator types can service this floor
    /// </summary>
    public bool RequiresVip { get; init; }
    
    /// <summary>
    /// List of elevator types allowed to stop at this floor
    /// </summary>
    public IReadOnlyList<Enums.ElevatorType> AllowedElevatorTypes { get; init; } = Array.Empty<Enums.ElevatorType>();
}
