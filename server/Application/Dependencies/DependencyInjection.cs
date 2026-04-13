using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Helper;
using Application.Helpers;
using Application.Common.Interfaces;
using Application.Features;
using Application.Behaviors;
using Application.Common;

namespace Application.Dependencies;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.Scan(scan => scan.FromAssemblyOf<ApplicationAssemblyMarker>() // Using CursorHelper as a marker for the assembly
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(Application.Common.Interfaces.IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        // Decorators for the Custom Handlers
        services.Decorate(typeof(ICommandHandler<>), typeof(Application.Behaviors.ValidationDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(Application.Behaviors.ValidationDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(Application.Behaviors.ValidationDecorator.QueryHandler<,>));

        services.Decorate(typeof(ICommandHandler<>), typeof(Application.Behaviors.PermissionDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(Application.Behaviors.PermissionDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(Application.Behaviors.PermissionDecorator.QueryHandler<,>));

        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        // Cursor Helper
        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddSingleton<CursorHelper>();

        // Workspace Context
        services.AddScoped<WorkspaceContext>();

        // Handler Dispatcher - Using explicit registration to resolve CS0246
        services.AddScoped<IHandler, HandlerDispatcher>();

        return services;
    }
}
