using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities.Relationship;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler(
    IDataBase db, 
    WorkspaceContext context,
    HybridCache cache, 
    IRealtimeService realtime
) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken ct)
    {
        // 1. Permission Check
        // Redundant null check removed; PermissionDecorator guarantees context.CurrentMember exists
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.Workspaces.ById(request.workspaceId).FirstOrDefaultAsync(ct);
        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        // 2. Logic
        var existingUserIds = await db.WorkspaceMembers
            .AsNoTracking()
            .ByWorkspace(request.workspaceId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var newUserIds = request.userIds.Except(existingUserIds).ToList();
        if (!newUserIds.Any()) return Result.Success();

        foreach (var userId in newUserIds)
        {
            var member = WorkspaceMember.Create(
                userId: userId,
                projectWorkspaceId: request.workspaceId,
                role: request.role,
                status: MembershipStatus.Active,
                invitedById: context.CurrentMember.UserId,
                inviteMethod: "Email"
            );
            workspace.AddMember(member, context.CurrentMember.UserId);
        }

        await db.SaveChangesAsync(ct);

        // 3. Cache Invalidation
        foreach (var userId in newUserIds)
        {
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), ct);
        }
        await cache.RemoveByTagAsync(CacheConstants.Tags.WorkspaceMembers(request.workspaceId), ct);

        // 4. Notifications
        _ = realtime.NotifyUsersAsync(newUserIds, "AddedToWorkspace", new { WorkspaceId = workspace.Id }, ct);

        return Result.Success();
    }
}
