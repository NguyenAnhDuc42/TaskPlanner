using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Application.Helpers;
using Application.Interfaces;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.UpdateView;

public class UpdateViewHandler :BaseFeatureHandler, IRequestHandler<UpdateViewCommand, Unit>
{
    public UpdateViewHandler(IUnitOfWork unitOfWork, WorkspaceContext workspaceContext, ICurrentUserService currentUserService) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(UpdateViewCommand request, CancellationToken cancellationToken)
    {
        var view = await UnitOfWork.Set<ViewDefinition>().FindAsync(request.Id);
        if (view == null) throw new KeyNotFoundException("View not found.");

        view.Update(request.Name,request.IsDefault);
        
        return Unit.Value;
    }
}
