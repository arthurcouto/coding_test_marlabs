using Elevator.Domain.Interfaces;
using Elevator.Core.Elevators;
using Elevator.Core.Infrastructure;
using Elevator.Core.Metrics;
using Elevator.Core.Scheduling;
using Elevator.Core.System;
using Microsoft.Extensions.DependencyInjection;

namespace Elevator.Core.Extensions;

/// <summary>
/// Configures dependencies for the Elevator System
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElevatorSystem(this IServiceCollection services, Domain.Entities.SystemSettings settings)
    {
        services.AddSingleton(settings);
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IMetricsCollector, InMemoryMetricsCollector>();
        
        // We can register a specific scheduler or allow the consumer to override
        services.AddTransient<IScheduler, ClosestElevatorScheduler>();
        
        services.AddSingleton<IElevatorSystem, ElevatorSystem>();

        return services;
    }

    /// <summary>
    /// Helper to register elevators with the DI container
    /// </summary>
    public static IServiceCollection AddElevators(this IServiceCollection services, params Domain.Entities.ElevatorConfiguration[] configurations)
    {
        foreach (var config in configurations)
        {
            services.AddSingleton<IElevator>(sp => 
            {
                var clock = sp.GetRequiredService<IClock>();
                return config.Type switch
                {
                    Domain.Enums.ElevatorType.Express => new ExpressElevator(config, clock),
                    Domain.Enums.ElevatorType.Freight => new FreightElevator(config, clock),
                    _ => new Elevators.Elevator(config, clock)
                };
            });
        }
        
        return services;
    }
}
