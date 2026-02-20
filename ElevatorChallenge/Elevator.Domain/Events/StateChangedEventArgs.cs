using Elevator.Domain.Enums;

namespace Elevator.Domain.Events;

/// <summary>
/// Event arguments for when an elevator changes state
/// </summary>
public class StateChangedEventArgs : EventArgs
{
    public Guid ElevatorId { get; }
    public ElevatorState OldState { get; }
    public ElevatorState NewState { get; }

    public StateChangedEventArgs(Guid elevatorId, ElevatorState oldState, ElevatorState newState)
    {
        ElevatorId = elevatorId;
        OldState = oldState;
        NewState = newState;
    }
}
