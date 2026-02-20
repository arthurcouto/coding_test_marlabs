using Elevator.Core.Scheduling;
using Elevator.Core.System;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Elevator.Core.Metrics;
using Elevator.Core.Extensions;

namespace Elevator.Tests.Integration;

public class FloorRestrictionTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IElevatorSystem _system;

    public FloorRestrictionTests()
    {
        var services = new ServiceCollection();
        var settings = new SystemSettings { MinFloor = 1, MaxFloor = 20, LocalElevatorCount = 0, ExpressElevatorCount = 0, FreightElevatorCount = 0 };
        services.AddElevatorSystem(settings);

        // Configure 1 Local (All Floors), 1 Express (10-20), 1 Freight (1-5)
        var configs = new[]
        {
            new ElevatorConfiguration { Type = ElevatorType.Local, FloorTravelTime = TimeSpan.FromMilliseconds(5), DoorOperationTime = TimeSpan.FromMilliseconds(5) },
            new ElevatorConfiguration { Type = ElevatorType.Express, AllowedFloors = new HashSet<int>(Enumerable.Range(10, 11)), FloorTravelTime = TimeSpan.FromMilliseconds(5), DoorOperationTime = TimeSpan.FromMilliseconds(5) },
            new ElevatorConfiguration { Type = ElevatorType.Freight, AllowedFloors = new HashSet<int> { 1, 2, 3, 4, 5 }, FloorTravelTime = TimeSpan.FromMilliseconds(5), DoorOperationTime = TimeSpan.FromMilliseconds(5) }
        };
        
        services.AddElevators(configs);
        _serviceProvider = services.BuildServiceProvider();
        _system = _serviceProvider.GetRequiredService<IElevatorSystem>();
    }

    [Fact]
    public void Dispatcher_ShouldAssignFreight_OnlyWhenWithinAllowedFloors()
    {
        // Assert initial state
        var elevators = _system.GetElevators().ToList();
        var local = elevators.Single(e => e.Configuration.Type == ElevatorType.Local);
        var express = elevators.Single(e => e.Configuration.Type == ElevatorType.Express);
        var freight = elevators.Single(e => e.Configuration.Type == ElevatorType.Freight);

        // Act: Request from Floor 1 (allowed for Local and Freight)
        // To guarantee Freight is chosen over Local, we put Local out of service temporarily
        local.EnterMaintenance();
        Thread.Sleep(50); // State changes async

        // Now Local is out of service. Express is out of bounds. Freight is idle at 1.
        _system.SubmitRequest(new Request(2, 4, Direction.Up));
        
        // Polling wait for the async dispatcher
        int maxRetries = 10;
        while (freight.PendingCount == 0 && freight.CurrentFloor == 1 && maxRetries-- > 0)
        {
            Thread.Sleep(100);
        }

        // Assert: Freight should have picked it up because 2 and 4 are in its AllowedFloors (1-5)
        Assert.True(freight.PendingCount > 0 || freight.CurrentFloor > 1, "Freight elevator did not pick up the allowed request.");
    }

    [Fact]
    public void Dispatcher_ShouldBlockFreight_WhenRequestExceedsAllowedFloors()
    {
        var elevators = _system.GetElevators().ToList();
        var local = elevators.Single(e => e.Configuration.Type == ElevatorType.Local);
        var freight = elevators.Single(e => e.Configuration.Type == ElevatorType.Freight);

        // Move Local away
        local.AddDestination(new Request(8, 8, Direction.None));
        Thread.Sleep(50);

        // Act: Request from Floor 1 to Floor 10
        // Floor 1 is allowed for Freight, but Floor 10 is NOT.
        _system.SubmitRequest(new Request(1, 10, Direction.Up));
        Thread.Sleep(50);

        // Assert: Freight must strictly ignore it. Only Local can take it.
        Assert.Equal(0, freight.PendingCount);
        Assert.Equal(1, freight.CurrentFloor); // Freight never moved
        Assert.True(local.PendingCount > 0 || local.CurrentFloor != 8); // Local was forced to come get it
    }

    [Fact]
    public void Dispatcher_ShouldAssignExpress_OnlyWhenWithinAllowedFloors()
    {
        var elevators = _system.GetElevators().ToList();
        var local = elevators.Single(e => e.Configuration.Type == ElevatorType.Local);
        var express = elevators.Single(e => e.Configuration.Type == ElevatorType.Express);
        
        // Express spawn floor is 10 (minimum of its AllowedFloors).

        // Act: Request from 10 to 20
        _system.SubmitRequest(new Request(10, 20, Direction.Up));
        Thread.Sleep(50);

        // Assert: Express should take it because it is exactly in its zone and it spawns at 10 natively
        Assert.True(express.PendingCount > 0 || express.CurrentFloor > 10);
        Assert.Equal(0, local.PendingCount); // Local should be ignored since Express is closer
    }

    [Fact]
    public void Dispatcher_ShouldBlockExpress_WhenRequestExceedsAllowedFloors()
    {
        var elevators = _system.GetElevators().ToList();
        var local = elevators.Single(e => e.Configuration.Type == ElevatorType.Local);
        var express = elevators.Single(e => e.Configuration.Type == ElevatorType.Express);

        // Act: Request from 8 to 15
        // 15 is allowed for Express, but 8 is NOT (Express is 10-20)
        _system.SubmitRequest(new Request(8, 15, Direction.Up));
        Thread.Sleep(50);

        // Assert: Express must strictly ignore it.
        Assert.Equal(0, express.PendingCount);
        Assert.Equal(10, express.CurrentFloor); // Express never moved from its spawn
        Assert.True(local.PendingCount > 0 || local.CurrentFloor > 1); // Local had to take it
    }

    [Fact]
    public void Dispatcher_ShouldAssignExpress_EvenWhenFurtherAway_IfAllowed()
    {
        var elevators = _system.GetElevators().ToList();
        var local = elevators.Single(e => e.Configuration.Type == ElevatorType.Local);
        var express = elevators.Single(e => e.Configuration.Type == ElevatorType.Express);
        
        // Setup:
        // Local is at 1. Express is at 10.
        // We request from 1 to 10.
        // For distance: Local is distance 0. Express is distance 9!
        // But since this request starts inside Express bounds (1) and goes to Express bounds (10), 
        // the new Scoring algorithmic massive Express bonus (-10000) should mathematically force the Express to come all the way down to get it,
        // leaving the Local free for shorter non-express trips.
        
        // Express config allowed floors: 10,11
        // We must change the express config for this test or use a request within its bounds.
        // In the original config for this test file, Express only allowed 10 and 11.
        // Let's modify the Express allowed floors for this test specifically by sending a request from 10 to 11 when Local is at 10.
        
        local.AddDestination(new Request(10, 10, Direction.None));
        express.AddDestination(new Request(11, 11, Direction.None));
        Thread.Sleep(50);
        
        // Now Local is at 10 (Distance 0). Express is at 11 (Distance 1).
        
        _system.SubmitRequest(new Request(10, 11, Direction.Up));
        
        int maxRetries = 10;
        while (express.PendingCount == 0 && express.CurrentFloor == 11 && maxRetries-- > 0)
        {
            Thread.Sleep(100);
        }

        // Output should be Express handles it despite Local being right there.
        Assert.True(express.PendingCount > 0 || express.CurrentFloor < 11, "Express elevator did not pick up the Express-oriented request.");
        Assert.Equal(0, local.PendingCount);
    }
}
