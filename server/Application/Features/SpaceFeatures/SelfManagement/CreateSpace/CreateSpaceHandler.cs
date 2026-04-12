using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public class CreateSpaceHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateSpaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        // 1. Permission Check
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        // 2. Fractional Index Calculation
        var maxKey = await db.Spaces
            .AsNoTracking()
            .ByWorkspace(context.workspaceId)
            .WhereNotDeleted()
            .MaxAsync(s => (string?)s.OrderKey, ct);
        
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);

        var slug = SlugHelper.GenerateSlug(request.name);
        var customization = Customization.Create(request.color, request.icon);

        // 3. Create Space
        var space = ProjectSpace.Create(
            projectWorkspaceId: context.workspaceId,
            name: request.name,
            slug: slug,
            description: request.description,
            customization: customization,
            isPrivate: request.isPrivate,
            creatorId: context.CurrentMember.Id,
            orderKey: orderKey
        );

        await db.Spaces.AddAsync(space, ct);
        await db.SaveChangesAsync(ct);
        
        return Result<Guid>.Success(space.Id);
    }
}
