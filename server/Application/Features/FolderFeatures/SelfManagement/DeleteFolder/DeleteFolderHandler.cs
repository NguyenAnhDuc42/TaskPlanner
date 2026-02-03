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
        var folder = await FindOrThrowAsync<ProjectFolder>(request.FolderId);

        // Check if folder has child lists
        var hasLists = await UnitOfWork.Set<ProjectList>()
            .AnyAsync(l => l.ProjectFolderId == folder.Id && !l.IsArchived, cancellationToken);

        if (hasLists)
        {
            throw new InvalidOperationException("Cannot delete folder that contains active lists. Archive or move the lists first.");
        }

        folder.SoftDelete();

        return Unit.Value;
    }
}
