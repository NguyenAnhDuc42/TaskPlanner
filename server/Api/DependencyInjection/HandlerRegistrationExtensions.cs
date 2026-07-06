using System.Reflection;

namespace Api;

public static class HandlerRegistrationExtensions
{
     public static IServiceCollection AddHandlerPipeline(this IServiceCollection services)
    {
        var handlerAssemblies = new[] { Assembly.GetExecutingAssembly() };

        services.Scan(scan => scan.FromAssemblies(handlerAssemblies)
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

        foreach (var assembly in handlerAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly);
        }

        services.Decorate(typeof(ICommandHandler<>), typeof(PipelineDecorator.CommandBaseHandler<>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(PipelineDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(PipelineDecorator.QueryHandler<,>));

        return services;
    }
}
