using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.DeleteDashboard;

public class DeleteDashboardHandler : BaseFeatureHandler, IRequestHandler<DeleteDashboardCommand, Unit>
{
    public DeleteDashboardHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext){}

    public async Task<Unit> Handle(DeleteDashboardCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);
        if (dashboard.IsMain) throw new InvalidOperationException("Cannot delete the main dashboard.");

        dashboard.SoftDelete();
        return Unit.Value;
    }
}
