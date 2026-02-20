using System.Collections.Concurrent;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.System;

/// <summary>
/// Internal dispatcher that continuously monitors a queue of requests
/// and delegates them to the provided Scheduler.
/// </summary>
public class RequestDispatcher
{
    private readonly BlockingCollection<Request> _requestQueue = new();
    private readonly IScheduler _scheduler;
    private readonly IEnumerable<IElevator> _elevators;
    private readonly SystemSettings _settings;

    public RequestDispatcher(IScheduler scheduler, IEnumerable<IElevator> elevators, SystemSettings settings)
    {
        _scheduler = scheduler;
        _elevators = elevators;
        _settings = settings;
    }

    public void EnqueueRequest(Request request)
    {
        _requestQueue.Add(request);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        
        try
        {
            foreach (var request in _requestQueue.GetConsumingEnumerable(cancellationToken))
            {
                Dispatch(request);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Dispatch(Request request)
    {
        if ((DateTime.UtcNow - request.Timestamp).TotalMilliseconds > _settings.GlobalTimeoutMs)
        {
            Console.WriteLine($"[DLQ] Request {request.Id} dropped. Exceeded GlobalTimeoutMs of {_settings.GlobalTimeoutMs}ms waiting for an elevator.");
            return;
        }

        var validElevatorsForRequest = _elevators.Where(e => 
            e.State != ElevatorState.OutOfService &&
            e.State != ElevatorState.Maintenance &&
            (!e.Configuration.AllowedFloors.Any() || 
             (e.Configuration.AllowedFloors.Contains(request.PickupFloor) && 
              e.Configuration.AllowedFloors.Contains(request.DestinationFloor))));

        var selectedElevator = _scheduler.SelectElevator(request, validElevatorsForRequest);
        
        if (selectedElevator != null)
        {
            selectedElevator.AddDestination(request);
        }
        else
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                EnqueueRequest(request);
            });
        }
    }
}
