using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces.Services.Permissions;
using Application.Common;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : BaseCommandHandler, IRequestHandler<AddMembersCommand, Guid>
{
    private readonly HybridCache _cache;

    public AddMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, HybridCache cache)
       : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
        _cache = cache;
    }

    public async Task<Guid> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        // 1. Authorize & Fetch using the new unified logic
        var workspace = await AuthorizeAndFetchAsync<ProjectWorkspace>(request.workspaceId, PermissionAction.Create, cancellationToken);

        var normalizedMembers = request.members
            .DistinctBy(m => m.email.Trim().ToLowerInvariant())
            .Where(m => !string.IsNullOrWhiteSpace(m.email))
            .Select(m => new
            {
                NormalizedEmail = m.email.Trim().ToLowerInvariant(),
                m.role
            })
            .ToList();

        if (normalizedMembers.Count == 0) return workspace.Id;

        var emailsToFind = normalizedMembers
            .Select(m => m.NormalizedEmail)
            .ToList();

        var users = await UnitOfWork.Set<User>()
            .Where(u => emailsToFind.Contains(u.Email.ToLower()))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var usersByNormalizedEmail = users.ToDictionary(
                keySelector: u => u.Email.Trim().ToLowerInvariant(),
                elementSelector: u => u);

        // Fetch ALL members including soft-deleted ones to check for re-adds
        var existingMembers = await UnitOfWork.Set<WorkspaceMember>()
            .IgnoreQueryFilters()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId)
            .ToListAsync(cancellationToken);

        var specs = new List<(Guid UserId, Role Role, MembershipStatus Status, string? JoinMethod)>();

        foreach (var member in normalizedMembers)
        {
            if(!usersByNormalizedEmail.TryGetValue(member.NormalizedEmail, out var user)) continue;

            var existing = existingMembers.FirstOrDefault(m => m.UserId == user.Id);
            if (existing != null)
            {
                if (existing.DeletedAt != null)
                {
                    existing.UpdateRole(member.role);
                    existing.RestoreMember();
                }
                continue;
            }

            specs.Add((user.Id, member.role, MembershipStatus.Active, "Invite"));
        }

        if (specs.Count > 0)
        {
            workspace.AddMembers(specs, CurrentUserId);
            await _cache.RemoveByTagAsync(CacheConstants.Tags.WorkspaceMembers(request.workspaceId), cancellationToken);
        }

        return workspace.Id;
    }
}
