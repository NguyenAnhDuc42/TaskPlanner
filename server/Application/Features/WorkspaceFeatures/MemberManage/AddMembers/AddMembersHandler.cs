using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : ICommandHandler<AddMembersCommand, Guid>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public AddMembersHandler(IDataBase db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(AddMembersCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure<Guid>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces
            .AsNoTracking()
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure<Guid>(WorkspaceError.NotFound);
        
    
        var normalizedMembers = request.members
            .DistinctBy(m => m.email.Trim().ToLowerInvariant())
            .Where(m => !string.IsNullOrWhiteSpace(m.email))
            .ToList();

        if (normalizedMembers.Count == 0) return Result.Success(workspace.Id);

        // 🟢 Optimized Bulk Process using Raw SQL
        // 1. Find User IDs by Email
        var emails = normalizedMembers.Select(m => m.email.Trim().ToLowerInvariant()).ToList();
        var usersSql = "SELECT id, email FROM users WHERE LOWER(email) = ANY(@Emails)";
        var users = await _db.QueryAsync<(Guid Id, string Email)>(usersSql, new { Emails = emails }, cancellationToken: ct);
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

            await _db.ExecuteAsync(upsertSql, new
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                UserId = userId,
                Role = member.role.ToString(), // Assuming Role is enum mapped as string or adjust accordingly
                CreatorId = currentUserId
            }, cancellationToken: ct);
        }

        return Result.Success(workspace.Id);
    }
}
