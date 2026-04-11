using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Interfaces.Data;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public class DeleteFolderHandler : ICommandHandler<DeleteFolderCommand>
{
    private readonly IDataBase _db;

    public DeleteFolderHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken ct)
    {
        var folder = await _db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) return Result.Failure(FolderError.NotFound);

        // Check if folder has active tasks
        var hasTasks = await _db.Tasks
            .AsNoTracking()
            .ByFolder(folder.Id)
            .WhereActive()
            .AnyAsync(ct);

        if (hasTasks) return Result.Failure(FolderError.HasActiveTasks);

        folder.SoftDelete();
        await _db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
