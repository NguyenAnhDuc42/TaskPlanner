using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Interfaces.Data;

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public class CreateFolderHandler : ICommandHandler<CreateFolderCommand, Guid>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceContext _workspaceContext;

    public CreateFolderHandler(IDataBase db, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    {
        _db = db;
        _currentUserService = currentUserService;
        _workspaceContext = workspaceContext;
    }

    public async Task<Result<Guid>> Handle(CreateFolderCommand request, CancellationToken ct)
    {
        var space = await _db.Spaces.ById(request.spaceId).FirstOrDefaultAsync(ct);
        if (space == null) return Result.Failure<Guid>(SpaceError.NotFound);
        
        var customization = Customization.Create(request.color, request.icon);

        var maxKey = await _db.Folders
            .AsNoTracking()
            .BySpace(request.spaceId)
            .WhereNotDeleted()
            .MaxAsync(f => (string?)f.OrderKey, ct);
        
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
        var slug = SlugHelper.GenerateSlug(request.name);

        var folder = ProjectFolder.Create(
            projectSpaceId: space.Id,
            name: request.name,
            slug: slug,
            description: request.description,
            orderKey: orderKey,
            isPrivate: request.isPrivate,
            creatorId: _currentUserService.CurrentUserId(),
            customization: customization,
            startDate: request.startDate,
            dueDate: request.dueDate
        );

        await _db.Folders.AddAsync(folder, ct);
        await _db.SaveChangesAsync(ct);
        
        return folder.Id;
    }
}
