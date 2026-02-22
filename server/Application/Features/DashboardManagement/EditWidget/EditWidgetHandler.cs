using System;
using System.Text.Json;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardManagement.EditWidget;

public class EditWidgetHandler : BaseFeatureHandler, IRequestHandler<EditWidgetCommand, Unit>
{
    public EditWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditWidgetCommand request, CancellationToken cancellationToken)
    {
        var widget = await FindOrThrowAsync<Widget>(request.widgetId);

        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        var configJson = JsonSerializer.Serialize(request.filter);
        widget.UpdateConfig(configJson);

        UnitOfWork.Set<Widget>().Update(widget);

        return Unit.Value;
    }
}
