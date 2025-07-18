using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using src.Domain.Enums;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly PlannerDbContext _context;
    private readonly IMemoryCache _cache;
    public PermissionService(PlannerDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }
    public async Task<Role> GetUserRole(Guid userId, Guid workspaceId)
    {
        var cacheKey = $"workspace_role_{userId}_{workspaceId}";
        if (_cache.TryGetValue(cacheKey, out Role userRole))
        {
            return userRole;
        }
        var roleFromDb = await _context.UserWorkspaces
            .Where(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId)
            .Select(uw => (Role?)uw.Role)
            .FirstOrDefaultAsync();

        var finalRole = roleFromDb ?? Role.Guest;
        if (roleFromDb.HasValue)
        {
            _cache.Set(cacheKey, finalRole, TimeSpan.FromMinutes(15));
        }
        return finalRole;
    }
}