using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.Widget;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.CreateWidget;

public class CreateWidgetHandler : BaseFeatureHandler, IRequestHandler<CreateWidgetCommand, Unit>
{
    public CreateWidgetHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(CreateWidgetCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        dashboard.AddWidget(
            widgetType: request.widgetType,
            configJson: "{}", // Default empty config
            visibility: WidgetVisibility.Public,
            col: request.Col,
            row: request.Row,
            width: request.Width,
            height: request.Height,
            creatorId: CurrentUserId
        );

        return Unit.Value;
    }
}
