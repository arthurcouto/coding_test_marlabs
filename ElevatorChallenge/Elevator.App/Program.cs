using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Elevator.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        
        // Remove default noisy logs for CLI clarity, leaving only Warnings/Errors
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices((context, services) =>
        {
            var settings = context.Configuration.Get<SystemSettings>() ?? new SystemSettings();
            services.AddElevatorSystem(settings);
            
            var expressFloors = ParseFloors(settings.ExpressAllowedFloors);
            var freightFloors = ParseFloors(settings.FreightAllowedFloors);

            var elevatorConfigs = new List<ElevatorConfiguration>();
            for (int i = 0; i < settings.LocalElevatorCount; i++)
            {
                elevatorConfigs.Add(new ElevatorConfiguration { Type = ElevatorType.Local, FloorTravelTime = TimeSpan.FromSeconds(1), DoorOperationTime = TimeSpan.FromSeconds(2) });
            }
            for (int i = 0; i < settings.ExpressElevatorCount; i++)
            {
                elevatorConfigs.Add(new ElevatorConfiguration { Type = ElevatorType.Express, FloorTravelTime = TimeSpan.FromSeconds(0.5), DoorOperationTime = TimeSpan.FromSeconds(2), AllowedFloors = expressFloors });
            }
            for (int i = 0; i < settings.FreightElevatorCount; i++)
            {
                elevatorConfigs.Add(new ElevatorConfiguration { Type = ElevatorType.Freight, FloorTravelTime = TimeSpan.FromSeconds(2), DoorOperationTime = TimeSpan.FromSeconds(4), AllowedFloors = freightFloors });
            }

            services.AddElevators(elevatorConfigs.ToArray());
            
            // Register hosted console interface
            services.AddHostedService<ConsoleInterface>();
        });

        var app = builder.Build();
        await app.RunAsync();
    }

    private static HashSet<int> ParseFloors(string floorsStr)
    {
        if (string.IsNullOrWhiteSpace(floorsStr)) return new HashSet<int>();
        
        var hashSet = new HashSet<int>();
        var parts = floorsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int floor))
            {
                hashSet.Add(floor);
            }
        }
        return hashSet;
    }
}
