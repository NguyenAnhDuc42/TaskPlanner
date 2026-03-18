using System;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.DeleteWidget;

public class DeleteWidgetHandler : BaseFeatureHandler, IRequestHandler<DeleteWidgetCommand, Unit>
{
    public DeleteWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        dashboard.RemoveWidget(request.widgetId);

        UnitOfWork.Set<Dashboard>().Update(dashboard);

        return Unit.Value;

    }
}
