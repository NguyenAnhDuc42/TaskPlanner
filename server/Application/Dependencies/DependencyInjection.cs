using System;
using Application.Helpers.WidgetTool;
using Application.Helpers.WidgetTool.WidgetQueryBuilder;
using Application.Pipeline;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Dependencies;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services,IConfiguration config)
    {

        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        services.AddScoped<WidgetBuilder>();
        services.AddSingleton<WidgetGridValidator>();

        return services;
    }
}
