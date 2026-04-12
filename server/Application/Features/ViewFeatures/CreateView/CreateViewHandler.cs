using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Domain.Enums;

namespace Application.Features.ViewFeatures.CreateView;

public class CreateViewHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateViewCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateViewCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role < Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        var view = new View
        {
            Id = Guid.NewGuid(),
            Name = "New View",
            LayerId = request.LayerId,
            LayerType = request.LayerType,
            ViewType = ViewType.List,
            CreatorId = context.CurrentMember.Id
        };

        db.Views.Add(view);
        await db.SaveChangesAsync(ct);

        return Result<Guid>.Success(view.Id);
    }
}