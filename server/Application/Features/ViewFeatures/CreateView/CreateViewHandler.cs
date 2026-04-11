using Application.Helpers;
using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.CreateView;

public class CreateViewHandler : ICommandHandler<CreateViewCommand, Guid>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public CreateViewHandler(IDataBase db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateViewCommand request, CancellationToken ct)
    {
        // 1. Layer Existence Validation
        var error = request.LayerType switch
        {
            EntityLayerType.ProjectWorkspace => await _db.Workspaces.AnyAsync(x => x.Id == request.LayerId, ct) ? null : WorkspaceError.NotFound,
            EntityLayerType.ProjectSpace => await _db.Spaces.AnyAsync(x => x.Id == request.LayerId, ct) ? null : SpaceError.NotFound,
            EntityLayerType.ProjectFolder => await _db.Folders.AnyAsync(x => x.Id == request.LayerId, ct) ? null : FolderError.NotFound,
            _ => Error.Validation("Layer.Invalid", "Invalid layer type.")
        };

        if (error != null) return error;

        var currentUserId = _currentUserService.CurrentUserId();

        var view = ViewDefinition.Create(
            request.LayerId, 
            request.LayerType, 
            request.Name, 
            request.ViewType,
            currentUserId,
            request.IsDefault);

        await _db.Views.AddAsync(view, ct);
        await _db.SaveChangesAsync(ct);
        
        return view.Id;
    }
}