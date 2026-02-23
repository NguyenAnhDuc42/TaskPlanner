using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.CreateView;


public class CreateViewHandler : BaseFeatureHandler, IRequestHandler<CreateViewCommand, Guid>
{
    public CreateViewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateViewCommand request, CancellationToken cancellationToken)
    {
        // Ownership/Existence check
        await GetLayer(request.LayerId, request.LayerType);

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