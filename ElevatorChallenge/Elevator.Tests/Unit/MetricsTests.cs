using Elevator.Core.Metrics;
using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Events;

namespace Elevator.Tests.Unit;

public class MetricsTests
{
    [Fact]
    public void InMemoryMetricsCollector_ShouldAggregateDataCorrectly()
    {
        // Arrange
        var collector = new InMemoryMetricsCollector();
        var request = new Request(1, 5, Direction.Up);
        var elevatorId = Guid.NewGuid();

        // Act
        collector.OnRequestCompleted(this, new RequestCompletedEventArgs(request, elevatorId, TimeSpan.FromSeconds(10)));
        collector.OnRequestCompleted(this, new RequestCompletedEventArgs(request, elevatorId, TimeSpan.FromSeconds(20)));

        // Assert
        var snapshot = collector.GetSnapshot();
        Assert.Equal(2, snapshot.RequestsCompleted);
        Assert.Equal(TimeSpan.FromSeconds(15), snapshot.AverageWaitTime); 
        Assert.Equal(2, snapshot.ElevatorUtilization[elevatorId]);
    }
}
