using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;
using Elevator.Core.Elevators;
using Moq;

namespace Elevator.Tests.Unit;

public class ElevatorTests
{
    [Fact]
    public void Elevator_ShouldInitializeCorrectly()
    {
        // Arrange
        var config = new ElevatorConfiguration { Type = ElevatorType.Local };
        var clockMock = new Mock<IClock>();

        // Act
        var elevator = new Core.Elevators.Elevator(config, clockMock.Object);

        // Assert
        Assert.Equal(ElevatorState.Idle, elevator.State);
        Assert.Equal(1, elevator.CurrentFloor);
        Assert.Equal(0, elevator.PendingCount);
    }
    
    [Fact]
    public void Elevator_AddDestination_ShouldTriggerStateChangeIfIdle()
    {
        // Arrange
        var config = new ElevatorConfiguration { Type = ElevatorType.Local };
        var clockMock = new Mock<IClock>();
        var elevator = new Core.Elevators.Elevator(config, clockMock.Object);
        var request = new Request(5, 10, Direction.Up);
        
        bool eventFired = false;
        elevator.StateChanged += (sender, args) => eventFired = true;

        // Act
        elevator.AddDestination(request);

        // Assert
        Assert.True(eventFired);
        Assert.NotEqual(ElevatorState.Idle, elevator.State);
        Assert.Equal(1, elevator.PendingCount);
    }

    [Fact]
    public void Elevator_EnterMaintenance_ShouldChangeStateToMaintenance()
    {
        // Arrange
        var config = new ElevatorConfiguration { Type = ElevatorType.Local };
        var clockMock = new Mock<IClock>();
        var elevator = new Core.Elevators.Elevator(config, clockMock.Object);

        // Act
        elevator.EnterMaintenance();

        // Assert
        Assert.Equal(ElevatorState.Maintenance, elevator.State);
    }
}
