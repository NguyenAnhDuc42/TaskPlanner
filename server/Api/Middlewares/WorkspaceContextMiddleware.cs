using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Api;

public class WorkspaceContextMiddleware
{
    private readonly RequestDelegate _next;
    public WorkspaceContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TaskPlanDbContext db)
    {
        var workspaceId = ExtractWorkspaceId(context);
        if (!workspaceId.HasValue)
        {
            await _next(context);
            return;
        }

        context.Items["WorkspaceId"] = workspaceId.Value;

        var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? context.User?.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var member = await db.WorkspaceMembers
                .AsNoTracking()
                .Where(m => m.ProjectWorkspaceId == workspaceId.Value && m.UserId == userId && m.DeletedAt == null)
                .Select(m => new { m.Status })
                .FirstOrDefaultAsync();

            if (member == null)
            {
                await WriteForbidden(context, "You are not a member of this workspace.");
                return;
            }

            if (member.Status != MembershipStatus.Active)
            {
                var detail = member.Status switch
                {
                    MembershipStatus.Suspended => "Your membership has been suspended.",
                    MembershipStatus.Pending => "Your membership is pending admin approval.",
                    MembershipStatus.Invited => "You haven't accepted your invitation to this workspace yet.",
                    _ => "You do not have active access to this workspace."
                };
                await WriteForbidden(context, detail);
                return;
            }
        }

        await _next(context);
    }

  
    private const string AccessDeniedCode = "workspace_access_denied";

    private static Task WriteForbidden(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/403",
            title = "Forbidden",
            detail,
            status = StatusCodes.Status403Forbidden,
            code = AccessDeniedCode
        });
    }

    public static Guid? ExtractWorkspaceId(HttpContext context)
    {
        var value = context.Request.Headers["X-Workspace-Id"].FirstOrDefault()
                ?? context.Request.RouteValues["workspaceId"]?.ToString()
                ?? context.Request.Query["workspaceId"].ToString();
        return Guid.TryParse(value, out var workspaceId) ? workspaceId : null;
    }

}

