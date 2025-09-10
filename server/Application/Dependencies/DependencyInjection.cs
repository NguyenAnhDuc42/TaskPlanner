using System;
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
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
