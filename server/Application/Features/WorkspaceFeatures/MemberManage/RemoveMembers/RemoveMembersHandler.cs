using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler : ICommandHandler<RemoveMembersCommand, Guid>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public RemoveMembersHandler(IDataBase db, ICurrentUserService currentUserService) 
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(RemoveMembersCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure<Guid>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces
            .AsNoTracking()
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure<Guid>(WorkspaceError.NotFound);

        if (request.memberIds.Any())
        {
            var sql = @"
                UPDATE workspace_members 
                SET deleted_at = NOW(), 
                    updated_at = NOW() 
                WHERE project_workspace_id = @WorkspaceId 
                  AND user_id = ANY(@UserIds)
                  AND deleted_at IS NULL";

            await _db.ExecuteAsync(sql, new
            {
                WorkspaceId = workspace.Id,
                UserIds = request.memberIds.ToArray()
            }, cancellationToken: cancellationToken);
        }

        return Result.Success(workspace.Id);
    }
}
