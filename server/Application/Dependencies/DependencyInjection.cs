using Application.Features.DashboardFeatures.WidgetDataHelper;
using Application.Pipeline;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Helper;
using Application.Helpers;
using Application.Features.ViewFeatures.FeatureHelpers;
namespace Application.Dependencies;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services,IConfiguration config)
    {

        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.Scan(scan => scan.FromAssembliesOf<ApplicationAssemblyMarker>()
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

        // Decorators for the NEW Custom Handlers (Running parallel with old MediatR pipelines)
        services.Decorate(typeof(ICommandHandler<>), typeof(Application.Behaviors.ValidationDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(Application.Behaviors.ValidationDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(Application.Behaviors.ValidationDecorator.QueryHandler<,>));

        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
 
        // Widget Tools
        services.AddScoped<WidgetBuilder>();

        // Cursor Helper
        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddSingleton<CursorHelper>();

        // Workspace Context
        services.AddScoped<WorkspaceContext>();

        // View Engine
        services.AddScoped<ViewBuilder>();

        return services;
    }
}
