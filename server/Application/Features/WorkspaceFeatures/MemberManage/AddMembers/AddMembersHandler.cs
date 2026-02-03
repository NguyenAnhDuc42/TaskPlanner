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
using Application.Common;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : BaseFeatureHandler, IRequestHandler<AddMembersCommand, Guid>
{
    public AddMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Guid> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.workspaceId);

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

        // Fetch existing members (including soft-deleted) to handle re-adds
        // Note: ProjectWorkspace._members handles tracking if loaded via Include,
        // but here we are fetching them separately for convenience.
        // Actually, we should load them via the aggregate if possible, but the current 
        // setup uses many-to-many relationships that might not be fully mapped in EF as Nav properties.
        
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
        }

        return workspace.Id;
    }
}
