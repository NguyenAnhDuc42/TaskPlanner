using System;
using Application.Interfaces.RealTime;
using Infrastructure.Services.RealTime;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations;

public static class RealTimeRegistration
{
    public static IServiceCollection AddRealtimeServices(this IServiceCollection services)
    {
        services.AddSingleton<IRealtimePublisher, RealtimePublisher>();
        return services;
    }
}
