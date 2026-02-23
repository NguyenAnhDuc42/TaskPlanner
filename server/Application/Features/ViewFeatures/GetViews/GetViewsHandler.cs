using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.GetViews;

public class GetViewsHandler : BaseFeatureHandler, IRequestHandler<GetViewsQuery, List<ViewDto>>
{
    public GetViewsHandler(IUnitOfWork unitOfWork, WorkspaceContext workspaceContext, ICurrentUserService currentUserService) 
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<ViewDto>> Handle(GetViewsQuery request, CancellationToken cancellationToken)
    {
        return await UnitOfWork.Set<ViewDefinition>()
            .Where(v => v.LayerId == request.LayerId && v.LayerType == request.LayerType)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new ViewDto(
                v.Id,
                v.Name,
                v.ViewType,
                v.IsDefault
            ))
            .ToListAsync(cancellationToken);
    }
}
