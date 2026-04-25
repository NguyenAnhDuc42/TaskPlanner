using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures;

public class CreateViewHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateViewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateViewCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        var view = ViewDefinition.Create(
            context.workspaceId,
            request.LayerId,
            request.LayerType,
            request.Name,
            request.ViewType,
            context.CurrentMember.Id 
        );

        await db.ViewDefinitions.AddAsync(view, ct);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(view.Id);
    }
}