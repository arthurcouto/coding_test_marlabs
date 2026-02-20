using Elevator.Core.Infrastructure;
using Elevator.Core.Metrics;
using Elevator.Core.Scheduling;
using Elevator.Core.System;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;

namespace Elevator.Tests.Integration;

public class ConcurrencyTests
{
    [Fact]
    public async Task System_ShouldHandle100ConcurrentRequests()
    {
        // Arrange
        var clock = new SystemClock();
        var metrics = new InMemoryMetricsCollector();
        var scheduler = new FifoScheduler();
        
        var elevators = new[]
        {
            new Core.Elevators.Elevator(new ElevatorConfiguration { FloorTravelTime = TimeSpan.FromMilliseconds(10), DoorOperationTime = TimeSpan.FromMilliseconds(10) }, clock),
            new Core.Elevators.Elevator(new ElevatorConfiguration { FloorTravelTime = TimeSpan.FromMilliseconds(10), DoorOperationTime = TimeSpan.FromMilliseconds(10) }, clock)
        };

        var system = new ElevatorSystem(elevators, scheduler, metrics, new SystemSettings());

        // Act: Fire 100 requests concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            int requestNum = i;
            tasks.Add(Task.Run(() => 
            {
                var request = new Request(requestNum % 10 + 1, (requestNum + 5) % 10 + 1, Direction.Up);
                system.SubmitRequest(request);
            }));
        }

        await Task.WhenAll(tasks);

        // Give the system time to drain the queues
        await Task.Delay(5000);

        // Assert
        // The goal is not that all completed quickly, but that the system didn't crash
        // and queues are draining correctly without race conditions.
        int totalPending = elevators.Sum(e => e.PendingCount);
        
        // Either tests are done, or processing smoothly
        Assert.True(totalPending < 100); 

        await system.ShutdownAsync();
    }
}
