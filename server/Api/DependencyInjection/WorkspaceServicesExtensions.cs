namespace Api;

public static class WorkspaceServicesExtensions
{
    public static IServiceCollection AddWorkspaceServices(this IServiceCollection services)
    {
        services.AddScoped<WorkspaceContext>();
        services.AddScoped<WorkspaceService>();
        services.AddScoped<WorkspaceMembershipResolver>();

        return services;
    }
}
