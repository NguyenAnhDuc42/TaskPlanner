using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.DeleteWidget;

public class DeleteWidgetHandler : BaseFeatureHandler, IRequestHandler<DeleteWidgetCommand, Unit>
{
    public DeleteWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);
        dashboard.RemoveWidget(request.widgetId);
        return Unit.Value;
    }
}
