using Application.Features;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.GetStatusList;

public class GetStatusListHandler : BaseFeatureHandler, IRequestHandler<GetStatusListQuery, List<StatusDto>>
{
    public GetStatusListHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<StatusDto>> Handle(GetStatusListQuery request, CancellationToken cancellationToken)
    {
        // Use direct Find instead of FindOrThrow for cleaner resolution
        var workflow = await UnitOfWork.Set<Workflow>().FindAsync(request.WorkflowId, cancellationToken);
        if (workflow == null) throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found");

        var statuses = await UnitOfWork.Set<Status>()
            .Where(s => s.WorkflowId == request.WorkflowId && s.DeletedAt == null)
            .OrderBy(s => s.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return statuses.Select(s => new StatusDto(
            s.Id,
            s.Name,
            s.Color,
            s.Category
        )).ToList();
    }
}
