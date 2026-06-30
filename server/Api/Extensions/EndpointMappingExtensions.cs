using System.Reflection;

namespace Api;

public static class EndpointMappingExtensions
{
    /// <summary>
    /// Scans the given assembly for static classes exposing a public static
    /// `MapEndpoint(IEndpointRouteBuilder)` method (minimal-API slice convention)
    /// and invokes each one, so new endpoints are wired up automatically.
    /// </summary>
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
