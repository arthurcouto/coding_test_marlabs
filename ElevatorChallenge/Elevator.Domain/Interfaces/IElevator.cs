using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Events;

namespace Elevator.Domain.Interfaces;

/// <summary>
/// Defines the contract for an elevator in the system
/// </summary>
public interface IElevator
{
    Guid Id { get; }
    int CurrentFloor { get; }
    ElevatorState State { get; }
    int PendingCount { get; }
    ElevatorConfiguration Configuration { get; }

    /// <summary>
    /// Adds a destination request to this elevator's queue
    /// </summary>
    void AddDestination(Request request);

    /// <summary>
    /// Puts the elevator into maintenance mode
    /// </summary>
    void EnterMaintenance();
    
    /// <summary>
    /// Immediately halts the elevator, clears queue and overrides all states
    /// </summary>
    void EmergencyStop();
    
    /// <summary>
    /// Brings the elevator back into service
    /// </summary>
    void ExitMaintenance();

    event EventHandler<FloorReachedEventArgs> FloorReached;
    event EventHandler<StateChangedEventArgs> StateChanged;
    event EventHandler<RequestCompletedEventArgs> RequestCompleted;
}
