using Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;
using Application.Interfaces.Services.Permissions;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Permission;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Services.Permissions;

public class PermissionProvider : IPermissionProvider
{
    private readonly TaskPlanDbContext _dbContext;
    private readonly HybridCache _cache;
    public PermissionProvider(TaskPlanDbContext dbContext,HybridCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }
    public async Task<PermissionContext> GetPermissionsFor(Guid userId, Guid workspaceId, CancellationToken ct)
    {
        var member = await _dbContext.WorkspaceMembers.AsNoTracking()
           .FirstOrDefaultAsync(x => x.UserId == userId && x.ProjectWorkspaceId == workspaceId,cancellationToken: ct)
           ?? throw new InvalidOperationException($"User {userId} not member of workspace {workspaceId}");
        return await LoadAccessesAndBuildContext(userId, member.Id, member.Role, ct);

    }
    private async Task<PermissionContext> LoadAccessesAndBuildContext(
        Guid userId,
        Guid workspaceMemberId,
        Role role,
        CancellationToken ct)
    {
        var accesses = await _dbContext.EntityAccesses
            .AsNoTracking()
            .Where(x => x.WorkspaceMemberId == workspaceMemberId)
            .Select(x => new
            {
                x.EntityLayer,
                x.EntityId,
                x.AccessLevel
            })
            .ToListAsync(ct);

        return new PermissionContext(
            userId: userId,
            workspaceMemberId: workspaceMemberId,
            role: role,
            accesses: accesses.Select(x => ((EntityLayerType)x.EntityLayer,x.EntityId,(AccessLevel)x.AccessLevel)));
    }
}