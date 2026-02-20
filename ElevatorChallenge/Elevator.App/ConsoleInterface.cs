using Elevator.Domain.Entities;
using Elevator.Domain.Enums;
using Elevator.Domain.Interfaces;
using Elevator.Core.Metrics;
using Microsoft.Extensions.Hosting;

namespace Elevator.App;

public class ConsoleInterface : BackgroundService
{
    private readonly IElevatorSystem _system;
    private readonly IMetricsCollector _metrics;
    private readonly IHostApplicationLifetime _lifetime;

    public ConsoleInterface(IElevatorSystem system, IMetricsCollector metrics, IHostApplicationLifetime lifetime)
    {
        _system = system;
        _metrics = metrics;
        _lifetime = lifetime;
        
        // Subscribe to events for comprehensive logging
        foreach (var elevator in _system.GetElevators())
        {
            elevator.FloorReached += (s, e) => Console.WriteLine($"\n[LOG] Elevator {e.ElevatorId.ToString()[..4]} reached floor {e.Floor}");
            elevator.StateChanged += (s, e) => Console.WriteLine($"\n[LOG] Elevator {e.ElevatorId.ToString()[..4]} changed state to {e.NewState}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for system to initialize slightly
        await Task.Delay(500, stoppingToken);

        Console.Clear();
        Console.WriteLine("=========================================");
        Console.WriteLine("       Elevator System Challenge         ");
        Console.WriteLine("=========================================");
        Console.WriteLine("Commands:");
        Console.WriteLine(" req [pickup] [dest] [isVip] - Request an elevator (e.g., req 1 5)");
        Console.WriteLine(" req [pickup] [dest] vip     - Request VIP elevator (e.g., req 1 5 vip)");
        Console.WriteLine(" emergency [1..N]            - Halt specific elevator index (e.g., emergency 1)");
        Console.WriteLine(" status                      - View elevators status");
        Console.WriteLine(" metrics                     - View system metrics");
        Console.WriteLine(" q                           - Quit");
        Console.WriteLine("=========================================\n");

        // The console reading loop runs on a separate thread to not block ExecuteAsync
        _ = Task.Run(() => ReadCommands(stoppingToken), stoppingToken);
    }

    private void ReadCommands(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input)) continue;

            var parts = input.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0];

            try
            {
                switch (command)
                {
                    case "req":
                        HandleRequest(parts);
                        break;
                    case "emergency":
                        HandleEmergency(parts);
                        break;
                    case "status":
                        PrintStatus();
                        break;
                    case "metrics":
                        PrintMetrics();
                        break;
                    case "q":
                    case "quit":
                    case "exit":
                        Console.WriteLine("Shutting down...");
                        _lifetime.StopApplication();
                        return;
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing command: {ex.Message}");
            }
        }
    }

    private void HandleRequest(string[] parts)
    {
        if (parts.Length < 3)
        {
            Console.WriteLine("Invalid args. Usage: req [pickup] [dest] [optional: vip]");
            return;
        }

        if (!int.TryParse(parts[1], out int pickup) || !int.TryParse(parts[2], out int dest))
        {
            Console.WriteLine("Floors must be numbers.");
            return;
        }

        bool isVip = parts.Length > 3 && parts[3] == "vip";
        
        var direction = pickup < dest ? Direction.Up : 
                        pickup > dest ? Direction.Down : Direction.None;

        var request = new Request(pickup, dest, direction, isVip);
        _system.SubmitRequest(request);
        
        Console.WriteLine($"Request submitted to dispatcher.");
    }

    private void PrintStatus()
    {
        Console.WriteLine("\n--- Elevator Status ---");
        var elevators = _system.GetElevators().ToList();
        
        for (int i = 0; i < elevators.Count; i++)
        {
            var e = elevators[i];
            string allowed = e.Configuration.AllowedFloors.Any() 
                ? string.Join(",", e.Configuration.AllowedFloors.OrderBy(x => x)) 
                : "All";
            Console.WriteLine($"Elevator {i+1} [Type: {e.Configuration.Type}] | Floor: {e.CurrentFloor} | State: {e.State} | Queue: {e.PendingCount} | Allowed: {allowed}");
        }
        Console.WriteLine("-----------------------\n");
    }

    private void PrintMetrics()
    {
        if (_metrics is not InMemoryMetricsCollector collector)
        {
            Console.WriteLine("Metrics snapshot unavailable.");
            return;
        }

        var snapshot = collector.GetSnapshot();
        Console.WriteLine("\n--- System Metrics ---");
        Console.WriteLine($"Requests Completed: {snapshot.RequestsCompleted}");
        Console.WriteLine($"Avg Wait Time: {snapshot.AverageWaitTime.TotalSeconds:F1}s");
        Console.WriteLine($"Avg Trip Time: {snapshot.AverageTripTime.TotalSeconds:F1}s");
        Console.WriteLine("-----------------------\n");
    }

    private void HandleEmergency(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out int index))
        {
            Console.WriteLine("Invalid args. Usage: emergency [index 1-N]");
            return;
        }

        var elevators = _system.GetElevators().ToList();
        if (index < 1 || index > elevators.Count)
        {
            Console.WriteLine("Elevator index out of range.");
            return;
        }

        var elevator = elevators[index - 1];
        elevator.EmergencyStop();
        Console.WriteLine($"[!] EMERGENCY STOP triggered for Elevator {index}. Queue dropped.");
    }
}
