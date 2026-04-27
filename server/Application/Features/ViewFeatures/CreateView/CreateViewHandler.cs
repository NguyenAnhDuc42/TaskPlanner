using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures;

public class CreateViewHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateViewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateViewCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        Guid? spaceId = request.LayerType == EntityLayerType.ProjectSpace ? request.LayerId : null;
        Guid? folderId = request.LayerType == EntityLayerType.ProjectFolder ? request.LayerId : null;

        var view = ViewDefinition.Create(
            context.workspaceId,
            spaceId,
            folderId,
            request.Name,
            request.ViewType,
            context.CurrentMember.Id
        );

        await db.ViewDefinitions.AddAsync(view, ct);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(view.Id);
    }
}