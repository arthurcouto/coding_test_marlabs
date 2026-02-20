using Elevator.Core.Metrics;
using Elevator.Core.Scheduling;
using Elevator.Core.System;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;
using Moq;

namespace Elevator.Tests.Unit;

public class ValidationTests
{
    [Theory]
    [InlineData(0, 5)]   // Below MinFloor
    [InlineData(11, 5)]  // Above MaxFloor
    [InlineData(5, 0)]   // Dest below MinFloor
    [InlineData(5, 11)]  // Dest above MaxFloor
    public void ElevatorSystem_ShouldRejectInvalidFloors(int pickup, int dest)
    {
        // Arrange
        var settings = new SystemSettings { MinFloor = 1, MaxFloor = 10 };
        var elevators = new List<IElevator>();
        var scheduler = new Mock<IScheduler>();
        var metrics = new Mock<IMetricsCollector>();
        
        var system = new ElevatorSystem(elevators, scheduler.Object, metrics.Object, settings);
        var request = new Request(pickup, dest, Direction.Up);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => system.SubmitRequest(request));
        Assert.Contains("invalid", ex.Message.ToLowerInvariant());
    }
}
