using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Application.Features.ViewFeatures.Logic;
using Application.Helpers;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.GetViewData;

public class GetViewDataHandler : BaseFeatureHandler, IRequestHandler<GetViewDataQuery, object>
{
    private readonly ViewBuilder _viewBuilder;

    public GetViewDataHandler(IUnitOfWork unitOfWork, ViewBuilder viewBuilder, WorkspaceContext workspaceContext, ICurrentUserService currentUserService) : base(unitOfWork, currentUserService, workspaceContext)
    {
        _viewBuilder = viewBuilder;
    }

    public async Task<object> Handle(GetViewDataQuery request, CancellationToken cancellationToken)
    {
        var view = await UnitOfWork.Set<ViewDefinition>().FindAsync(request.ViewId);
        if (view == null) throw new KeyNotFoundException("View not found.");

        return await _viewBuilder.Build(view.LayerId, view.LayerType, view.ViewType);
    }
}
