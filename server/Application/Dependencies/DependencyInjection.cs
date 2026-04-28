using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Helper;
using Application.Helpers;
using Application.Common.Interfaces;
using Application.Features;
using Application.Features.WorkspaceFeatures;
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
        );

        // Consolidated Pipeline Decorator (Logging + Validation + Permission)
        services.Decorate(typeof(ICommandHandler<>), typeof(PipelineDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PipelineDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(PipelineDecorator.QueryHandler<,>));

        // Cursor Helper
        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddSingleton<CursorHelper>();

        // Workspace Context
        services.AddScoped<WorkspaceContext>();
        services.AddScoped<WorkspaceService>();

        // Handler Dispatcher - Using explicit registration to resolve CS0246
        services.AddScoped<IHandler, HandlerDispatcher>();

        return services;
    }
}
