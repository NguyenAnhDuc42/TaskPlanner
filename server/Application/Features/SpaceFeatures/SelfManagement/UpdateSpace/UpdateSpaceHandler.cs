using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SpaceFeatures;

public class UpdateSpaceHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateSpaceCommand>
{
    public async Task<Result> Handle(UpdateSpaceCommand request, CancellationToken ct)
    {
        var space = await db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        // Security Resolve: Direct workspace bound check
        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        // AUTHORIZATION: Only Admin/Owner or the space creator (MemberId) can update space settings
        if (context.CurrentMember.Role > Role.Admin && space.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        // Apply Updates preserving domain logic
        if (request.Name is not null || request.Description is not null)
        {
            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
            space.UpdateBasicInfo(request.Name, slug, request.Description);
        }

        if (request.Color is not null || request.Icon is not null) 
            space.UpdateCustomization(request.Color, request.Icon);

        if (request.IsPrivate.HasValue) 
            space.UpdatePrivate(request.IsPrivate.Value);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
