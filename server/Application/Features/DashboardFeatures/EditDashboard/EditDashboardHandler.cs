using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.EditDashboard;

public class EditDashboardHandler : BaseFeatureHandler, IRequestHandler<EditDashboardCommand, Unit>
{
    public EditDashboardHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditDashboardCommand request, CancellationToken cancellationToken)
    {
        var dashboard = await FindOrThrowAsync<Dashboard>(request.dashboardId);

        dashboard.UpdateName(request.name);
        if (request.isShared.HasValue) dashboard.UpdateShared(request.isShared.Value);
        if (request.isMain.HasValue) dashboard.UpdateMain(request.isMain.Value);

        return Unit.Value;
    }
}