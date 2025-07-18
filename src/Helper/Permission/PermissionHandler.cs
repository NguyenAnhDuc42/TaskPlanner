using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using src.Domain.Enums;
using src.Helper.Permission;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Helper.Middleware;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IMemoryCache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHierarchyRepository _hierarchyRepository;

    public PermissionHandler(IMemoryCache cache, ICurrentUserService currentUserService, IHierarchyRepository hierarchyRepository)
    {
        _cache = cache;
        _currentUserService = currentUserService;
        _hierarchyRepository = hierarchyRepository;
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }
        var workspaceRoleClaim = user.FindFirst(CustomClaims.WorkspaceRole)?.Value;
        if (!Enum.TryParse<Role>(workspaceRoleClaim, out var role))
        {
            context.Fail();
            return;
        }
        if (!PermissionMappings.HasPermission(role, requirement.Permission))
        {
            context.Fail();
            return;
        }
        if (requirement.CheckCreator)
        {
            if (!await IsCreatorOrElevatedRole(context, role))
            {
                context.Fail();
                return;
            }
        }
        context.Succeed(requirement);
    }

    private async Task<bool> IsCreatorOrElevatedRole(AuthorizationHandlerContext context, Role role)
    {
        if (role == Role.Owner || role == Role.Admin) return true;

        var itemId = GetItemIdFromRoute(context.Resource);
        if (itemId == Guid.Empty)
        {
            return false;
        }

        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
        {
            return false;
        }

        return await IsItemCreatorAsync(userId, itemId);
    }

    private Guid GetItemIdFromRoute(object? resource)
    {
        if (resource is HttpContext httpContext && httpContext.Request.RouteValues.TryGetValue("id", out var idObj))
            return Guid.TryParse(idObj?.ToString(), out var id) ? id : Guid.Empty;
        return Guid.Empty;
    }

    private async Task<bool> IsItemCreatorAsync(Guid userId, Guid itemId)
    {
        return await _hierarchyRepository.IsOwnedByUser(itemId, userId);
    }
}
