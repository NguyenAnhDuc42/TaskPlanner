using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.CreateView;


public class CreateViewHandler : BaseFeatureHandler, IRequestHandler<CreateViewCommand, Guid>
{
    public CreateViewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateViewCommand request, CancellationToken cancellationToken)
    {
        // 1. Layer Existence Validation
        var layerExists = request.LayerType switch
        {
            EntityLayerType.ProjectWorkspace => await UnitOfWork.Set<ProjectWorkspace>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            EntityLayerType.ProjectSpace => await UnitOfWork.Set<ProjectSpace>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            EntityLayerType.ProjectFolder => await UnitOfWork.Set<ProjectFolder>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            EntityLayerType.ChatRoom => await UnitOfWork.Set<ChatRoom>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            _ => false
        };

        if (!layerExists) throw new KeyNotFoundException($"{request.LayerType} {request.LayerId} not found");

        var view = ViewDefinition.Create(
            request.LayerId, 
            request.LayerType, 
            request.Name, 
            request.ViewType,
            CurrentUserId,
            request.IsDefault);

        await UnitOfWork.Set<ViewDefinition>().AddAsync(view);
        return view.Id;
    }
}