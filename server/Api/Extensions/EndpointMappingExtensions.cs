using System.Reflection;

namespace Api;

public static class EndpointMappingExtensions
{
    public static void MapAllEndpoints(this IEndpointRouteBuilder app, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true })
            .Where(t => t.GetMethod("MapEndpoint", BindingFlags.Public | BindingFlags.Static) != null);

        foreach (var type in endpointTypes)
        {
            var method = type.GetMethod("MapEndpoint", BindingFlags.Public | BindingFlags.Static)!;
            method.Invoke(null, [app]);
        }
    }
}
