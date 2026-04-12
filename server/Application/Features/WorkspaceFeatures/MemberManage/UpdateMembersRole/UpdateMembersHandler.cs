using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembersRole;

public class UpdateMembersHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateMembersCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateMembersCommand request, CancellationToken ct)
    {
        // Permission: Only Admin/Owner can update member roles
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        var userIds = request.members.Select(m => m.userId).ToList();

        var membersToUpdate = await db.WorkspaceMembers
            .Where(m => m.ProjectWorkspaceId == context.workspaceId && userIds.Contains(m.UserId))
            .ToListAsync(ct);

        if (membersToUpdate.Count == 0) return Result<Guid>.Success(context.workspaceId);

        var memberMap = request.members.ToDictionary(m => m.userId);

        foreach (var memberEntity in membersToUpdate)
        {
            if (memberMap.TryGetValue(memberEntity.UserId, out var update))
            {
                memberEntity.UpdateMembershipDetails(
                    update.role ?? memberEntity.Role,
                    update.status ?? memberEntity.MembershipStatus
                );
            }
        }

        await db.SaveChangesAsync(ct);
        return Result<Guid>.Success(context.workspaceId);
    }
}
