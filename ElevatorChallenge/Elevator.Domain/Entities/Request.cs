using Elevator.Domain.Enums;

namespace Elevator.Domain.Entities;

/// <summary>
/// Represents an immutable request for an elevator
/// </summary>
public record Request
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The floor where the request was made
    /// </summary>
    public int PickupFloor { get; init; }
    
    /// <summary>
    /// The desired destination floor. Can be the same as pickup if this is just a hall call.
    /// </summary>
    public int DestinationFloor { get; init; }
    
    /// <summary>
    /// The intended direction of travel from the pickup floor
    /// </summary>
    public Direction Direction { get; init; }
    
    /// <summary>
    /// When the request was created
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional VIP flag for prioritized scheduling
    /// </summary>
    public bool IsVip { get; init; }

    public Request(int pickupFloor, int destinationFloor, Direction direction, bool isVip = false)
    {
        PickupFloor = pickupFloor;
        DestinationFloor = destinationFloor;
        Direction = direction;
        IsVip = isVip;
    }
}
