using System;
using Application.Helpers.WidgetTool;
using Application.Helpers.WidgetTool.WidgetQueryBuilder;
using Application.Pipeline;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Helper;
using Application.Helpers;
namespace Application.Dependencies;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services,IConfiguration config)
    {

        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Widget Tools
        services.AddScoped<WidgetBuilder>();
        services.AddSingleton<WidgetGridValidator>();

        // Cursor Helper
        services.Configure<CursorEncryptionOptions>(config.GetSection(CursorEncryptionOptions.SectionName));
        services.AddSingleton<CursorHelper>();

        // Workspace Context
        services.AddScoped<WorkspaceContext>();

        return services;
    }
}
