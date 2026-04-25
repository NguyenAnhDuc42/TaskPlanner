using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Dapper;

namespace Application.Features.WorkspaceFeatures;

public class RemoveMembersHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<RemoveMembersCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RemoveMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        if (request.memberIds.Any())
        {
            await db.Connection.ExecuteAsync(RemoveMembersSQL.RemoveMembers, new
            {
                WorkspaceId = context.workspaceId,
                UserIds = request.memberIds.ToArray()
            });
        }

        return Result<Guid>.Success(context.workspaceId);
    }
}
