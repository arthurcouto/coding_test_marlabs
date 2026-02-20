using System.Collections.Concurrent;
using Elevator.Domain.Entities;
using Elevator.Domain.Interfaces;
using Elevator.Core.Infrastructure;

namespace Elevator.Core.System;

/// <summary>
/// Orchestrates the entire elevator fleet, assigning requests to elevators via a Scheduler
/// </summary>
public class ElevatorSystem : IElevatorSystem, IDisposable
{
    private readonly IEnumerable<IElevator> _elevators;
    private readonly IScheduler _scheduler;
    private readonly IMetricsCollector _metrics;
    private readonly RequestDispatcher _dispatcher;
    private readonly SystemSettings _settings;
    
    // Cancellation used to stop the background workers on shutdown
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _backgroundTasks = new();

    public ElevatorSystem(
        IEnumerable<IElevator> elevators, 
        IScheduler scheduler, 
        IMetricsCollector metrics,
        SystemSettings settings)
    {
        _elevators = elevators;
        _scheduler = scheduler;
        _metrics = metrics;
        _settings = settings;
        
        // Setup internal request dispatcher
        _dispatcher = new RequestDispatcher(_scheduler, _elevators, _settings);
        
        // Attach system-level metrics listeners to all elevators
        foreach (var elevator in _elevators)
        {
            elevator.StateChanged += _metrics.OnStateChanged;
            elevator.RequestCompleted += _metrics.OnRequestCompleted;
            
            // Start the background loop for each elevator
            if (elevator is Elevators.Elevator realElevator)
            {
                _backgroundTasks.Add(Task.Run(() => realElevator.RunAsync(_cts.Token)));
            }
        }
        
        // Start the dispatcher loop
        _backgroundTasks.Add(Task.Run(() => _dispatcher.RunAsync(_cts.Token)));
    }

    public void SubmitRequest(Request request)
    {
        if (request.PickupFloor < _settings.MinFloor || request.PickupFloor > _settings.MaxFloor)
            throw new ArgumentException($"Pickup floor {request.PickupFloor} is invalid. Valid range: {_settings.MinFloor}-{_settings.MaxFloor}.");
            
        if (request.DestinationFloor < _settings.MinFloor || request.DestinationFloor > _settings.MaxFloor)
            throw new ArgumentException($"Destination floor {request.DestinationFloor} is invalid. Valid range: {_settings.MinFloor}-{_settings.MaxFloor}.");

        _dispatcher.EnqueueRequest(request);
    }

    public IEnumerable<IElevator> GetElevators()
    {
        return _elevators;
    }

    public async Task ShutdownAsync()
    {
        _cts.Cancel();
        await Task.WhenAll(_backgroundTasks);
    }

    public void Dispose()
    {
        foreach (var elevator in _elevators)
        {
            elevator.StateChanged -= _metrics.OnStateChanged;
            elevator.RequestCompleted -= _metrics.OnRequestCompleted;
        }
        _cts.Dispose();
    }
}
