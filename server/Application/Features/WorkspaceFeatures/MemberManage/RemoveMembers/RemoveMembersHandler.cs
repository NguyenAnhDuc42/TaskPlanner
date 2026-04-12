using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<RemoveMembersCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RemoveMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        if (request.memberIds.Any())
        {
            var sql = @"
                UPDATE workspace_members 
                SET deleted_at = NOW(), 
                    updated_at = NOW() 
                WHERE project_workspace_id = @WorkspaceId 
                  AND user_id = ANY(@UserIds)
                  AND deleted_at IS NULL";

            await db.ExecuteAsync(sql, new
            {
                WorkspaceId = context.workspaceId,
                UserIds = request.memberIds.ToArray()
            }, cancellationToken: ct);
        }

        return Result<Guid>.Success(context.workspaceId);
    }
}
