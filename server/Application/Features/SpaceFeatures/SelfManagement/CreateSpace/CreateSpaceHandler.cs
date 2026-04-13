using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public class CreateSpaceHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateSpaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Admin or Owner can create spaces
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        var maxKey = await db.Spaces
            .AsNoTracking()
            .ByWorkspace(context.workspaceId)
            .WhereNotDeleted()
            .MaxAsync(s => (string?)s.OrderKey, ct);
        
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);

        var slug = SlugHelper.GenerateSlug(request.name);
        var customization = Customization.Create(request.color, request.icon);

        var space = ProjectSpace.Create(
            projectWorkspaceId: context.workspaceId,
            name: request.name,
            slug: slug,
            description: request.description,
            customization: customization,
            isPrivate: request.isPrivate,
            creatorId: context.CurrentMember.Id, // REVERTED: Using MemberId
            orderKey: orderKey
        );

        await db.Spaces.AddAsync(space, ct);
        await db.SaveChangesAsync(ct);
        
        return Result<Guid>.Success(space.Id);
    }
}
