using Application.Contract.StatusContract;
using Application.Features;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.GetStatusList;

public class GetStatusListHandler : BaseFeatureHandler, IRequestHandler<GetStatusListQuery, List<StatusDto>>
{
    private readonly IStatusResolver _statusResolver;

    public GetStatusListHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        IStatusResolver statusResolver)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _statusResolver = statusResolver;
    }

    public async Task<List<StatusDto>> Handle(GetStatusListQuery request, CancellationToken cancellationToken)
    {
        await GetLayer(request.LayerId, request.LayerType);

        var (effectiveLayerId, effectiveLayerType) =
            await _statusResolver.ResolveEffectiveLayer(request.LayerId, request.LayerType);

        var statuses = await UnitOfWork.Set<Status>()
            .Where(s => s.LayerId == effectiveLayerId && s.LayerType == effectiveLayerType && s.DeletedAt == null)
            .OrderBy(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return statuses.Select(s => new StatusDto(
            s.Id,
            s.Name,
            s.Color,
            s.Category,
            s.IsDefaultStatus
        )).ToList();
    }
}
