using Elevator.Domain.Entities;

namespace Elevator.Domain.Interfaces;

/// <summary>
/// Orchestrator for the entire elevator system
/// </summary>
public interface IElevatorSystem
{
    /// <summary>
    /// Submits a new request from a user
    /// </summary>
    void SubmitRequest(Request request);
    
    /// <summary>
    /// Gets all elevators currently managed by the system
    /// </summary>
    IEnumerable<IElevator> GetElevators();
    
    /// <summary>
    /// Gracefully shuts down the system
    /// </summary>
    Task ShutdownAsync();
}
