using System;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using src.Domain.Enums;
using src.Helper.Permission; // This will likely need to be updated to src.Application.Permissions
using src.Infrastructure.Abstractions.IServices;
using Microsoft.AspNetCore.Http; // Add this using directive for HttpContext
using System.Threading.Tasks; // Add this using directive for Task

namespace src.Api.Middleware; // Updated namespace

public class WorkspaceClaimsMiddleware
{
    private readonly RequestDelegate _next;
    private const string WorkspaceIdHeaderName = "X-Workspace-Id";
    public WorkspaceClaimsMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context, IPermissionService permissionService, ICurrentUserService currentUserService)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var workspaceId = GetWorkspaceIdFromHeader(context);
            if (workspaceId != Guid.Empty)
            {
                await AddWorkspaceClaim(context, workspaceId, permissionService, currentUserService);
            }
        }
        await _next(context);
    }
    private Guid GetWorkspaceIdFromHeader(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(WorkspaceIdHeaderName, out var headerValue))
        {
            if (Guid.TryParse(headerValue.FirstOrDefault(), out var workspaceId))
            {
                return workspaceId;
            }
        }
        return Guid.Empty;
    }

    private async Task AddWorkspaceClaim(HttpContext context, Guid workspaceId, IPermissionService permissionService, ICurrentUserService currentUserService)
    {
        var userId = currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
        {
            return;
        }
        var role = await permissionService.GetUserRole(userId, workspaceId);
        if (context.User.Identity is not ClaimsIdentity identity)
        {
            // This should not happen with standard JWT/Cookie auth, but it's a safe check.
            return;
        }

        // Safely remove existing claims to prevent duplicates.
        // Calling RemoveClaim with a null argument (from FindFirst) throws an ArgumentNullException.
        var existingWorkspaceIdClaim = identity.FindFirst(CustomClaims.WorkspaceId);
        if (existingWorkspaceIdClaim != null) identity.RemoveClaim(existingWorkspaceIdClaim);

        var existingWorkspaceRoleClaim = identity.FindFirst(CustomClaims.WorkspaceRole);
        if (existingWorkspaceRoleClaim != null) identity.RemoveClaim(existingWorkspaceRoleClaim);

        identity.AddClaim(new Claim(CustomClaims.WorkspaceId, workspaceId.ToString()));
        identity.AddClaim(new Claim(CustomClaims.WorkspaceRole, role.ToString()));
    }
}