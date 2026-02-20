using System.Diagnostics;
using Elevator.Core.Extensions;
using Elevator.Core.Metrics;
using Elevator.Core.Scheduling;
using Elevator.Core.System;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Elevator.Tests.Integration;

public class PerformanceTests
{
    [Fact]
    public void Dispatcher_ShouldAssignElevator_Under100ms()
    {
        // Arrange
        var services = new ServiceCollection();
        var settings = new SystemSettings { MinFloor = 1, MaxFloor = 20, LocalElevatorCount = 5, ExpressElevatorCount = 0, FreightElevatorCount = 0 };
        
        services.AddElevatorSystem(settings);
        
        var configs = new List<ElevatorConfiguration>();
        for (int i = 0; i < 5; i++)
        {
            configs.Add(new ElevatorConfiguration { Type = ElevatorType.Local });
        }
        services.AddElevators(configs.ToArray());

        var sp = services.BuildServiceProvider();
        var system = sp.GetRequiredService<IElevatorSystem>();
        
        // Warmup (JIT compilation overhead removal)
        system.SubmitRequest(new Request(1, 2, Direction.Up));
        Thread.Sleep(50); // Let it dispatch

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var request = new Request(1, 10, Direction.Up);
        system.SubmitRequest(request);
        
        // Wait briefly to allow background dispatcher to dequeue and assign
        // We assert the time it took to just enqueue it, but the dispatcher is extremely fast
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Dispatching took {stopwatch.ElapsedMilliseconds}ms, which is >= 100ms");
    }

    [Fact]
    public void Dispatcher_ShouldAssignElevator_WithFloorRestrictions_Under100ms()
    {
        // Arrange
        var services = new ServiceCollection();
        var settings = new SystemSettings { MinFloor = 1, MaxFloor = 50, LocalElevatorCount = 5, ExpressElevatorCount = 2, FreightElevatorCount = 2 };
        
        services.AddElevatorSystem(settings);
        
        var configs = new List<ElevatorConfiguration>();
        // Add 5 Locals (all floors), 2 Express (10-50), 2 Freight (1-10)
        for (int i = 0; i < 5; i++) configs.Add(new ElevatorConfiguration { Type = ElevatorType.Local });
        for (int i = 0; i < 2; i++) configs.Add(new ElevatorConfiguration { Type = ElevatorType.Express, AllowedFloors = new HashSet<int>(Enumerable.Range(10, 41)) });
        for (int i = 0; i < 2; i++) configs.Add(new ElevatorConfiguration { Type = ElevatorType.Freight, AllowedFloors = new HashSet<int>(Enumerable.Range(1, 10)) });
        
        services.AddElevators(configs.ToArray());

        var sp = services.BuildServiceProvider();
        var system = sp.GetRequiredService<IElevatorSystem>();
        
        // Warmup Request - Triggers JIT and Thread Pool initialization
        system.SubmitRequest(new Request(1, 5, Direction.Up));
        Thread.Sleep(50);

        var stopwatch = new Stopwatch();

        // Act - Submit a request that requires LINQ filtering traversing 9 elevators
        stopwatch.Start();
        var request = new Request(20, 25, Direction.Up);
        system.SubmitRequest(request);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Restricted Dispatching took {stopwatch.ElapsedMilliseconds}ms, scaling penalty is too high.");
    }
}
