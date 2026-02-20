using Elevator.Domain.Entities;

namespace Elevator.Domain.Interfaces;

/// <summary>
/// Responsible for deciding which elevator should serve a given request
/// </summary>
public interface IScheduler
{
    /// <summary>
    /// Selects the best elevator to handle the request from the available fleet
    /// </summary>
    /// <param ref="request">The incoming request</param>
    /// <param ref="availableElevators">All operational elevators</param>
    /// <returns>The chosen elevator, or null if none can serve the request</returns>
    IElevator? SelectElevator(Request request, IEnumerable<IElevator> availableElevators);
}
