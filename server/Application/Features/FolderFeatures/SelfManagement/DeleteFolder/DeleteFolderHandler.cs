using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public class DeleteFolderHandler : BaseFeatureHandler, IRequestHandler<DeleteFolderCommand, Unit>
{
    public DeleteFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await UnitOfWork.Set<ProjectFolder>().FindAsync(request.FolderId, cancellationToken);
        if (folder == null) throw new KeyNotFoundException($"Folder {request.FolderId} not found");

        // Check if folder has child tasks
        var hasTasks = await UnitOfWork.Set<ProjectTask>()
            .AnyAsync(t => t.ProjectFolderId == folder.Id && !t.IsArchived, cancellationToken);

        if (hasTasks)
        {
            throw new InvalidOperationException("Cannot delete folder that contains active tasks. Archive or move the tasks first.");
        }

        folder.SoftDelete();

        return Unit.Value;
    }
}
