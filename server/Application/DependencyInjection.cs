using Application.Behaviors;
using Application.Common;
using Application.Common.Interfaces;
using Application.Features;
using Application.Features.WorkspaceFeatures;
using Application.Helper;
using Application.Helpers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        #region Handlers Registration
        services.Scan(scan => scan.FromAssemblyOf<ApplicationAssemblyMarker>()
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
        
        services.AddScoped<IHandler, HandlerDispatcher>();
        
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        #endregion

        #region Pipelines
        services.Decorate(typeof(ICommandHandler<>), typeof(PipelineDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PipelineDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(PipelineDecorator.QueryHandler<,>));
        #endregion

        #region Services & Contexts
        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddSingleton<CursorHelper>();

        services.AddScoped<WorkspaceContext>();
        services.AddScoped<WorkspaceService>();
        #endregion

        return services;
    }
}
