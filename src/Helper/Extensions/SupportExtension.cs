using System;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
namespace src.Helper.Extensions;

public static class SupportExtension
{
    public static IServiceCollection SupportExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters()
            .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
