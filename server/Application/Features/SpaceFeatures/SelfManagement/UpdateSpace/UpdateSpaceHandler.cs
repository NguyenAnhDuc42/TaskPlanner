using Application.Common.Errors;
using Application.Interfaces.Data;
using Application.Helpers;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public class UpdateSpaceHandler : ICommandHandler<UpdateSpaceCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceContext _workspaceContext;

    public UpdateSpaceHandler(IDataBase db, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    {
        _db = db;
        _currentUserService = currentUserService;
        _workspaceContext = workspaceContext;
    }

    public async Task<Result> Handle(UpdateSpaceCommand request, CancellationToken ct)
    {
        var space = await _db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) return Result.Failure(SpaceError.NotFound);

        var workspaceIdResult = _workspaceContext.TryGetWorkspaceId();
        if (workspaceIdResult.IsFailure) return workspaceIdResult;

        // 3. Apply Updates
        if (request.Name is not null || request.Description is not null)
        {
            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
            space.UpdateBasicInfo(request.Name, slug, request.Description);
        }

        if (request.Color is not null || request.Icon is not null) 
            space.UpdateCustomization(request.Color, request.Icon);

        if (request.IsPrivate.HasValue) 
            space.UpdatePrivate(request.IsPrivate.Value);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

