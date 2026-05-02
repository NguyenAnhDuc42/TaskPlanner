using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.SpaceFeatures;

public class CreateSpaceHandler(
    IDataBase db, 
    WorkspaceContext context,
    IRealtimeService realtime
) : ICommandHandler<CreateSpaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        await db.BeginTransactionAsync(ct);

        try
        {
            var maxKey = await db.Spaces
                .AsNoTracking()
                .ByWorkspace(context.workspaceId)
                .WhereNotDeleted()
                .MaxAsync(s => s.OrderKey, ct);
            
            var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
            var slug = SlugHelper.GenerateSlug(request.name);

            // 1. Create the primary document for this space
            var document = Document.Create(
                context.workspaceId,
                request.name, 
                context.CurrentMember.Id
            );
            await db.Documents.AddAsync(document, ct);

            // 2. Create the space linked to the document
            var space = ProjectSpace.Create(
                projectWorkspaceId: context.workspaceId,
                name: request.name,
                slug: slug,
                defaultDocumentId: document.Id,
                color: request.color,
                icon: request.icon,
                isPrivate: request.isPrivate,
                creatorId: context.CurrentMember.Id,
                orderKey: orderKey
            );

            await db.Spaces.AddAsync(space, ct);

            db.ViewDefinitions.AddRange(
                ViewDefinition.CreateDefaults(context.workspaceId, space.Id, null, context.CurrentMember.Id));

            await db.CommitTransactionAsync(ct);

            await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceCreated", new { SpaceId = space.Id, WorkspaceId = context.workspaceId }, ct);

            return Result<Guid>.Success(space.Id);
        }
        catch
        {
            await db.RollbackTransactionAsync(ct);
            throw;
        }
    }
}

