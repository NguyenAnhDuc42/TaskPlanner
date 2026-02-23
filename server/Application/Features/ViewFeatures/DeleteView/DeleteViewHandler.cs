using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Application.Helpers;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.DeleteView;

public class DeleteViewHandler : BaseFeatureHandler, IRequestHandler<DeleteViewCommand, Unit>
{
    public DeleteViewHandler(IUnitOfWork unitOfWork, WorkspaceContext workspaceContext, ICurrentUserService currentUserService) 
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteViewCommand request, CancellationToken cancellationToken)
    {
        var view = await UnitOfWork.Set<ViewDefinition>().FindAsync(request.Id);
        if (view == null) throw new KeyNotFoundException("View not found.");

        UnitOfWork.Set<ViewDefinition>().Remove(view);
        
        return Unit.Value;
    }
}
