namespace Elevator.Domain.Entities;

/// <summary>
/// Global system settings used for validation and initialization
/// </summary>
public record SystemSettings
{
    public int MinFloor { get; init; } = 1;
    public int MaxFloor { get; init; } = 10;
    public int LocalElevatorCount { get; init; } = 2;
    public int ExpressElevatorCount { get; init; } = 1;
    public int FreightElevatorCount { get; init; } = 1;
    public int GlobalTimeoutMs { get; init; } = 10000;
    
    // Arrays para definir os andares permitidos baseados em string via Environment Variables
    public string ExpressAllowedFloors { get; init; } = "";
    public string FreightAllowedFloors { get; init; } = "";
}
