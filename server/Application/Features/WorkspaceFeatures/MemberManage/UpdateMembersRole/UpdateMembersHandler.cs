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

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;

public class UpdateMembersHandler : ICommandHandler<UpdateMembersCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public UpdateMembersHandler(IDataBase db, ICurrentUserService currentUserService) {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateMembersCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workspace = await _db.Workspaces
            .AsNoTracking()
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        if (request.members == null || !request.members.Any()) return Result.Success();

        foreach (var memberUpdate in request.members)
        {
            var sql = @"
                UPDATE workspace_members 
                SET role = COALESCE(@Role, role), 
                    membership_status = COALESCE(@Status, membership_status), 
                    updated_at = NOW() 
                WHERE project_workspace_id = @WorkspaceId 
                  AND user_id = @UserId 
                  AND role != 'Owner' -- Protection for owners
                  AND deleted_at IS NULL";

            await _db.ExecuteAsync(sql, new
            {
                WorkspaceId = workspace.Id,
                UserId = memberUpdate.userId,
                Role = memberUpdate.role?.ToString(),
                Status = memberUpdate.status?.ToString()
            }, cancellationToken: cancellationToken);
        }

        return Result.Success();
    }
}
