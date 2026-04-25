using Application.Interfaces;
using Application.Interfaces.Data;
using Application.Interfaces.Services;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Services.Background;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Dependencies.Registrations;

public static class ServicesRegistration
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IDataBase, Database>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICookieService, CookieService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRealtimeService, SignalRRealtimeService>();
        services.AddTransient<IExternalAuthService, ExternalAuthService>();

        services.AddHttpContextAccessor();

        services.AddSingleton<IPasswordService, PasswordService>();

        services.AddTransient<Background.Interfaces.IBackgroundOutboxAccessor, Infrastructure.Services.Background.BackgroundOutboxAccessor>();
        services.AddTransient<Background.Interfaces.IBackgroundEventDispatcher, Infrastructure.Services.Background.BackgroundEventDispatcher>();
        services.AddTransient<Background.Interfaces.IBackgroundMemberCleanupStore, Infrastructure.Services.Background.BackgroundMemberCleanupStore>();
        
        services.AddSingleton(System.Threading.Channels.Channel.CreateUnbounded<bool>());
        services.AddHostedService<LocalOutboxWorker>();

        return services;
    }
}
