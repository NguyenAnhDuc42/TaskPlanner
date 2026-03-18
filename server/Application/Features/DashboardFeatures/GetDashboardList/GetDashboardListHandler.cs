using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.DashboardFeatures.GetDashboardList;

public class GetDashboardListHandler : BaseFeatureHandler, IRequestHandler<GetDashboardListQuery, List<DashboardDto>>
{
    public GetDashboardListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<DashboardDto>> Handle(GetDashboardListQuery request, CancellationToken cancellationToken)
    {
        return await UnitOfWork.Set<Dashboard>()
            .Where(d => d.LayerId == request.layerId && d.LayerType == request.layerType && d.DeletedAt == null)
            .OrderByDescending(d => d.IsMain)
            .ThenBy(d => d.Name)
            .Select(d => new DashboardDto(
                d.Id,
                d.Name,
                d.IsShared,
                d.IsMain,
                d.LayerType,
                d.LayerId))
            .ToListAsync(cancellationToken);
    }
}
