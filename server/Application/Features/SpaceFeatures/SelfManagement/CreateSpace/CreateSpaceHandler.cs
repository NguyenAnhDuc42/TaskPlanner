using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public class CreateSpaceHandler : ICommandHandler<CreateSpaceCommand, Guid>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceContext _workspaceContext;

    public CreateSpaceHandler(IDataBase db, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    {
        _db = db;
        _currentUserService = currentUserService;
        _workspaceContext = workspaceContext;
    }

    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        // 1. Resolve Workspace
        var workspaceIdResult = _workspaceContext.TryGetWorkspaceId();
        if (workspaceIdResult.IsFailure) return workspaceIdResult;

        var workspaceId = workspaceIdResult.Value;
        var currentUserId = _currentUserService.CurrentUserId();

        // 2. Validate Workspace existence
        var workspaceExists = await _db.Workspaces.AnyAsync(w => w.Id == workspaceId, ct);
        if (!workspaceExists) return Error.NotFound("Workspace.NotFound", $"Workspace {workspaceId} not found.");

        // 3. Fractional Index Calculation
        var maxKey = await _db.Spaces
            .AsNoTracking()
            .ByWorkspace(workspaceId)
            .WhereNotDeleted()
            .MaxAsync(s => (string?)s.OrderKey, ct);
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);

        var slug = SlugHelper.GenerateSlug(request.name);
        var customization = Customization.Create(request.color, request.icon);

        // 4. Create Space
        var space = ProjectSpace.Create(
            projectWorkspaceId: workspaceId,
            name: request.name,
            slug: slug,
            description: request.description,
            customization: customization,
            isPrivate: request.isPrivate,
            creatorId: currentUserId,
            orderKey: orderKey
        );

        await _db.Spaces.AddAsync(space, ct);
        await _db.SaveChangesAsync(ct);
        
        return space.Id;
    }
}
