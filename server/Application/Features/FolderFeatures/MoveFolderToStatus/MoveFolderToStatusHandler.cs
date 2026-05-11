using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.FolderFeatures;

public class MoveFolderToStatusHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<MoveFolderToStatusCommand>
{
    public async Task<Result> Handle(MoveFolderToStatusCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin) 
            return Result.Failure(MemberError.DontHavePermission);

        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, ct);

        var spaceId = await db.Folders
            .Where(f => f.Id == request.FolderId)
            .Select(f => f.ProjectSpaceId)
            .FirstOrDefaultAsync(ct);

        var affected = await db.Folders
            .Where(f => f.Id == request.FolderId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(f => f.StatusId, request.TargetStatusId)
                .SetProperty(f => f.OrderKey, newOrderKey)
                .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), ct);

        if (affected > 0)
        {
            await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderStatusChanged", new { 
                request.FolderId, 
                request.TargetStatusId, 
                NewOrderKey = newOrderKey,
                SpaceId = spaceId
            }, ct);
            return Result.Success();
        }

        return Result.Failure(Error.NotFound("Folder.NotFound", "Folder not found"));
    }

    private async Task<string> ResolveOrderKey(MoveFolderToStatusCommand request, CancellationToken ct)
    {
        if (request.PreviousItemOrderKey != null && request.NextItemOrderKey != null)
        {
            if (string.Compare(request.PreviousItemOrderKey, request.NextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(request.PreviousItemOrderKey);

            return FractionalIndex.Between(request.PreviousItemOrderKey, request.NextItemOrderKey);
        }

        if (request.PreviousItemOrderKey != null) return FractionalIndex.After(request.PreviousItemOrderKey);
        if (request.NextItemOrderKey != null) return FractionalIndex.Before(request.NextItemOrderKey);

        var maxKey = await db.Folders
            .Where(f => f.StatusId == request.TargetStatusId)
            .MaxAsync(f => f.OrderKey, ct);

        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }
}
