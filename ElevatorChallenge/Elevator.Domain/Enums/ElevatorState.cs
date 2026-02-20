namespace Elevator.Domain.Enums;

/// <summary>
/// Represents the operating state of an elevator
/// </summary>
public enum ElevatorState
{
    Idle,
    MovingUp,
    MovingDown,
    DoorsOpening,
    DoorsOpen,
    DoorsClosing,
    Maintenance,
    OutOfService
}
