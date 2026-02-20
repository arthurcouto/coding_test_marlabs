using Elevator.Domain.Entities;

namespace Elevator.Domain.Events;

/// <summary>
/// Event arguments for when a request is successfully fulfilled
/// </summary>
public class RequestCompletedEventArgs : EventArgs
{
    public Request Request { get; }
    public Guid ElevatorId { get; }
    
    // Duration from request creation to completion
    public TimeSpan Duration { get; }

    public RequestCompletedEventArgs(Request request, Guid elevatorId, TimeSpan duration)
    {
        Request = request;
        ElevatorId = elevatorId;
        Duration = duration;
    }
}
