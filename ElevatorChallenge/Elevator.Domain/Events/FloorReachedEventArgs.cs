namespace Elevator.Domain.Events;

/// <summary>
/// Event arguments for when an elevator reaches a specific floor
/// </summary>
public class FloorReachedEventArgs : EventArgs
{
    public Guid ElevatorId { get; }
    public int Floor { get; }

    public FloorReachedEventArgs(Guid elevatorId, int floor)
    {
        ElevatorId = elevatorId;
        Floor = floor;
    }
}
