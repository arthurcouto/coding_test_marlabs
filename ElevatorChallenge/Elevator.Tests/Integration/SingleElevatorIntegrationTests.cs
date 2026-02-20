using Elevator.Core.Elevators;
using Elevator.Core.Infrastructure;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Core.System;
using Elevator.Core.Extensions;
using Elevator.Core.Scheduling;
using Elevator.Core.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Elevator.Tests.Integration;

public class SingleElevatorIntegrationTests
{
    [Fact]
    public async Task Request_ShouldBeProcessedBySingleElevator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<Domain.Interfaces.IClock, SystemClock>();
        services.AddSingleton<Domain.Interfaces.IMetricsCollector, InMemoryMetricsCollector>();
        services.AddSingleton<Domain.Interfaces.IScheduler, FifoScheduler>();
        
        var settings = new SystemSettings { MinFloor = 1, MaxFloor = 15 };
        services.AddElevatorSystem(settings);
        
        var config = new ElevatorConfiguration { Type = ElevatorType.Local, FloorTravelTime = TimeSpan.FromMilliseconds(50), DoorOperationTime = TimeSpan.FromMilliseconds(50) };
        services.AddSingleton<Domain.Interfaces.IElevator>(sp => new Core.Elevators.Elevator(config, sp.GetRequiredService<Domain.Interfaces.IClock>()));

        var provider = services.BuildServiceProvider();
        var system = provider.GetRequiredService<Domain.Interfaces.IElevatorSystem>();
        var elevator = system.GetElevators().First();

        var request = new Request(1, 10, Direction.Up);

        // Act
        system.SubmitRequest(request);

        // Give the background tasks time to process the request
        await Task.Delay(2000); 

        // Assert
        Assert.Equal(ElevatorState.Idle, elevator.State);
        Assert.Equal(10, elevator.CurrentFloor);
        Assert.Equal(0, elevator.PendingCount);
        
        await system.ShutdownAsync();
    }
}
