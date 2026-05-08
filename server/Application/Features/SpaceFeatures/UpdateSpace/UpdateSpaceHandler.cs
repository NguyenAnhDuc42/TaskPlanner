using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SpaceFeatures;

public class UpdateSpaceHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<UpdateSpaceCommand>
{
    public async Task<Result> Handle(UpdateSpaceCommand request, CancellationToken ct)
    {
        var space = await db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        // Apply Updates preserving domain logic
        if (request.Name is not null)
        {
            space.UpdateName(request.Name);
            space.UpdateSlug(SlugHelper.GenerateSlug(request.Name));
        }

        if (request.Color is not null) space.UpdateColor(request.Color);
        if (request.Icon is not null) space.UpdateIcon(request.Icon);

        if (request.IsPrivate.HasValue) 
            space.UpdatePrivate(request.IsPrivate.Value);

        if (request.StartDate.HasValue)
            space.UpdateStartDate(request.StartDate.Value);

        if (request.DueDate.HasValue)
            space.UpdateDueDate(request.DueDate.Value);

        if (request.StatusId.HasValue)
            space.UpdateStatus(request.StatusId.Value);

        if (request.IsInheritingWorkflow.HasValue)
            space.UpdateInheritWorkflow(request.IsInheritingWorkflow.Value);

        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceUpdated", new { 
            SpaceId = space.Id, 
            WorkspaceId = context.workspaceId,
            Name = space.Name,
            Icon = space.Icon,
            Color = space.Color,
            StatusId = space.StatusId,
            IsPrivate = space.IsPrivate,
            StartDate = space.StartDate,
            DueDate = space.DueDate
        }, ct);

        return Result.Success();
    }
}
