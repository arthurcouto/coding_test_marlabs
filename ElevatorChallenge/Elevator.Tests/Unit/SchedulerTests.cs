using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;
using Elevator.Core.Scheduling;
using Moq;

namespace Elevator.Tests.Unit;

public class SchedulerTests
{
    [Fact]
    public void FifoScheduler_ShouldSelectIdleElevatorFirst()
    {
        // Arrange
        var scheduler = new FifoScheduler();
        
        var idleElevator = new Mock<IElevator>();
        idleElevator.Setup(e => e.State).Returns(ElevatorState.Idle);
        
        var busyElevator = new Mock<IElevator>();
        busyElevator.Setup(e => e.State).Returns(ElevatorState.MovingUp);
        
        var elevators = new[] { busyElevator.Object, idleElevator.Object };
        var request = new Request(1, 5, Direction.Up);

        // Act
        var selected = scheduler.SelectElevator(request, elevators);

        // Assert
        Assert.Same(idleElevator.Object, selected);
    }

    [Fact]
    public void ClosestElevatorScheduler_ShouldSelectClosestIdleElevator()
    {
        // Arrange
        var scheduler = new ClosestElevatorScheduler();
        
        var closeElevator = new Mock<IElevator>();
        closeElevator.Setup(e => e.State).Returns(ElevatorState.Idle);
        closeElevator.Setup(e => e.CurrentFloor).Returns(4);
        
        var farElevator = new Mock<IElevator>();
        farElevator.Setup(e => e.State).Returns(ElevatorState.Idle);
        farElevator.Setup(e => e.CurrentFloor).Returns(10);
        
        var elevators = new[] { farElevator.Object, closeElevator.Object };
        var request = new Request(5, 8, Direction.Up);

        // Act
        var selected = scheduler.SelectElevator(request, elevators);

        // Assert
        Assert.Same(closeElevator.Object, selected); // 4 is closer to 5 than 10
    }
}
