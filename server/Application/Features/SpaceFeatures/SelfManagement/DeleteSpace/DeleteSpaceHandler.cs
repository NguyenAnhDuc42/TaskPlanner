using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public class DeleteSpaceHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<DeleteSpaceCommand>
{
    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        // Security Resolve: Direct workspace bound check
        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        // AUTHORIZATION: Only Admin/Owner or the space creator (MemberId) can delete spaces
        if (context.CurrentMember.Role > Role.Admin && space.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        space.SoftDelete();
        
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
