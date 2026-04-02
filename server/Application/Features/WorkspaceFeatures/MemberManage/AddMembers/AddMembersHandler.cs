using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Common;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : BaseFeatureHandler, IRequestHandler<AddMembersCommand, Guid>
{
    public AddMembersHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Guid> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        // Use direct Find instead of FindOrThrow for cleaner resolution
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId, cancellationToken);

        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.workspaceId} not found");

        var normalizedMembers = request.members
            .DistinctBy(m => m.email.Trim().ToLowerInvariant())
            .Where(m => !string.IsNullOrWhiteSpace(m.email))
            .ToList();

        if (normalizedMembers.Count == 0) return workspace.Id;

        // 🟢 Optimized Bulk Process using Raw SQL
        // 1. Find User IDs by Email
        var emails = normalizedMembers.Select(m => m.email.Trim().ToLowerInvariant()).ToList();
        var usersSql = "SELECT id, email FROM users WHERE LOWER(email) = ANY(@Emails)";
        var users = await UnitOfWork.QueryAsync<(Guid Id, string Email)>(usersSql, new { Emails = emails }, cancellationToken);
        var userMap = users.ToDictionary(u => u.Email.ToLower(), u => u.Id);

        // 2. Separate into inserts and restores (Soft-delete cleanup)
        foreach (var member in normalizedMembers)
        {
            var emailLower = member.email.Trim().ToLowerInvariant();
            if (!userMap.TryGetValue(emailLower, out var userId)) continue;

            // Execute raw UPSERT for members to keep it clean and fast
            var upsertSql = @"
                INSERT INTO workspace_members (id, project_workspace_id, user_id, role, membership_status, join_method, creator_id, created_at, updated_at, deleted_at)
                VALUES (@Id, @WorkspaceId, @UserId, @Role, 'Active', 'Invite', @CreatorId, NOW(), NOW(), NULL)
                ON CONFLICT (project_workspace_id, user_id) 
                DO UPDATE SET 
                    deleted_at = NULL, 
                    role = EXCLUDED.role,
                    updated_at = NOW()
                WHERE workspace_members.deleted_at IS NOT NULL OR workspace_members.role != EXCLUDED.role;";

            await UnitOfWork.ExecuteAsync(upsertSql, new
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = member.role.ToString(), // Assuming Role is enum mapped as string or adjust accordingly
                CreatorId = CurrentUserId
            }, cancellationToken);
        }

        return workspace.Id;
    }
}
