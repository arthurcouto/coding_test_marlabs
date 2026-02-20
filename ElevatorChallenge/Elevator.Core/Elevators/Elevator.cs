using System.Collections.Concurrent;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Events;
using Elevator.Domain.Interfaces;

namespace Elevator.Core.Elevators;

/// <summary>
/// Operational implementation of an Elevator
/// </summary>
public class Elevator : IElevator
{
    public Guid Id { get; } = Guid.NewGuid();
    
    private int _currentFloor;
    public int CurrentFloor => _currentFloor;

    private ElevatorState _state = ElevatorState.Idle;
    public ElevatorState State => _state;

    public ElevatorConfiguration Configuration { get; }

    protected readonly PriorityQueue<Request, int> _destinations = new();
    
    public int PendingCount => _destinations.Count;

    public event EventHandler<FloorReachedEventArgs>? FloorReached;
    public event EventHandler<StateChangedEventArgs>? StateChanged;
    public event EventHandler<RequestCompletedEventArgs>? RequestCompleted;

    private readonly IClock _clock;
    private readonly object _stateLock = new();
    
    // Internal token source for interrupting active wait delays (e.g., door operation, moving delay)
    private CancellationTokenSource _operationCts = new();

    public Elevator(ElevatorConfiguration configuration, IClock clock)
    {
        Configuration = configuration;
        _clock = clock;
        
        // Spawn the elevator at the lowest available floor it is allowed to service
        if (configuration.AllowedFloors.Any())
        {
            _currentFloor = configuration.AllowedFloors.Min();
        }
        else
        {
            _currentFloor = 1; // Default to ground floor
        }
    }

    public void AddDestination(Request request)
    {
        lock (_stateLock)
        {
            int score = request.IsVip ? -10000 : 0;
            
            bool isGoingUp = State == ElevatorState.MovingUp || State == ElevatorState.DoorsClosing;
            bool isGoingDown = State == ElevatorState.MovingDown;
            
            if (isGoingUp && request.PickupFloor >= CurrentFloor && request.Direction == Direction.Up)
                score -= 5000;
            else if (isGoingDown && request.PickupFloor <= CurrentFloor && request.Direction == Direction.Down)
                score -= 5000;

            score += Math.Abs(CurrentFloor - request.PickupFloor);

            _destinations.Enqueue(request, score);
        }
        if (State == ElevatorState.Idle)
        {
            var direction = request.PickupFloor > CurrentFloor ? ElevatorState.MovingUp : 
                            request.PickupFloor < CurrentFloor ? ElevatorState.MovingDown : 
                            ElevatorState.DoorsOpening;
                            
            ChangeState(direction);
        }
    }

    public void EnterMaintenance()
    {
        lock (_stateLock)
        {
            ChangeState(ElevatorState.Maintenance);
            _operationCts.Cancel();
        }
    }

    public void EmergencyStop()
    {
        lock (_stateLock)
        {
            ChangeState(ElevatorState.OutOfService);
            _destinations.Clear();
            _operationCts.Cancel();
        }
    }

    public void ExitMaintenance()
    {
        lock (_stateLock)
        {
            if (State == ElevatorState.Maintenance || State == ElevatorState.OutOfService)
            {
                ChangeState(ElevatorState.Idle);
                if (_operationCts.IsCancellationRequested)
                {
                    _operationCts.Dispose();
                    _operationCts = new CancellationTokenSource();
                }
            }
        }
    }

    protected void ChangeState(ElevatorState newState)
    {
        ElevatorState oldState;
        lock (_stateLock)
        {
            if (_state == newState) return;
            oldState = _state;
            _state = newState;
        }

        StateChanged?.Invoke(this, new StateChangedEventArgs(Id, oldState, newState));
    }

    protected void UpdateFloor(int floor)
    {
        _currentFloor = floor;
        FloorReached?.Invoke(this, new FloorReachedEventArgs(Id, floor));
    }
    
    /// <summary>
    /// Processes the queue of destinations. Meant to be run in a background Task.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (State == ElevatorState.Maintenance || State == ElevatorState.OutOfService)
            {
                await _clock.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                continue;
            }

            Request? request = null;
            lock (_stateLock)
            {
                if (_destinations.Count > 0)
                {
                    request = _destinations.Dequeue();
                }
            }

            if (request != null)
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _operationCts.Token);
                try
                {
                    await ProcessRequestAsync(request, linkedCts.Token);
                }
                catch (OperationCanceledException) when (_operationCts.IsCancellationRequested)
                {
                }
            }
            else
            {
                ChangeState(ElevatorState.Idle);
                await _clock.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }
    }

    protected virtual async Task ProcessRequestAsync(Request request, CancellationToken cancellationToken)
    {
        await MoveToFloorAsync(request.PickupFloor, cancellationToken);
        await OperateDoorsAsync(cancellationToken);
        
        await MoveToFloorAsync(request.DestinationFloor, cancellationToken);
        await OperateDoorsAsync(cancellationToken);
        
        var duration = DateTime.UtcNow - request.Timestamp;
        RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(request, Id, duration));
    }

    private async Task MoveToFloorAsync(int targetFloor, CancellationToken cancellationToken)
    {
        if (CurrentFloor == targetFloor) return;

        var state = targetFloor > CurrentFloor ? ElevatorState.MovingUp : ElevatorState.MovingDown;
        ChangeState(state);

        while (CurrentFloor != targetFloor && !cancellationToken.IsCancellationRequested)
        {
            await _clock.Delay(Configuration.FloorTravelTime, cancellationToken);
            
            var nextFloor = CurrentFloor + (targetFloor > CurrentFloor ? 1 : -1);
            UpdateFloor(nextFloor);
        }
    }

    private async Task OperateDoorsAsync(CancellationToken cancellationToken)
    {
        ChangeState(ElevatorState.DoorsOpening);
        await _clock.Delay(Configuration.DoorOperationTime / 2, cancellationToken);
        
        ChangeState(ElevatorState.DoorsOpen);
        await _clock.Delay(Configuration.DoorOperationTime, cancellationToken); // Wait for passengers
        
        ChangeState(ElevatorState.DoorsClosing);
        await _clock.Delay(Configuration.DoorOperationTime / 2, cancellationToken);
    }
}
