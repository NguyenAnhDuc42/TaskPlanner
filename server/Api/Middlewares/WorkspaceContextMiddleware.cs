using System;

namespace Api.Middlewares;

public class WorkspaceContextMiddleware
{ 
    private readonly RequestDelegate _next;
    public WorkspaceContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
         var workspaceId = ExtractWorkspaceId(context);
         if (workspaceId.HasValue) context.Items["WorkspaceId"] = workspaceId.Value;
         await _next(context);
    } 

    public static Guid? ExtractWorkspaceId(HttpContext context)
    {
        var value = context.Request.Headers["X-Workspace-Id"].FirstOrDefault() 
                ?? context.Request.RouteValues["workspaceId"]?.ToString()
                ?? context.Request.Query["workspaceId"].ToString();
        return Guid.TryParse(value, out var workspaceId) ? workspaceId : null;
    }

}
