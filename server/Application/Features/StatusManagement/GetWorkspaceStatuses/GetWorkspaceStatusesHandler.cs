using Application.Features;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.GetWorkspaceStatuses;

public class GetWorkspaceStatusesHandler : BaseFeatureHandler, IRequestHandler<GetWorkspaceStatusesQuery, List<StatusDto>>
{
    public GetWorkspaceStatusesHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<StatusDto>> Handle(GetWorkspaceStatusesQuery request, CancellationToken cancellationToken)
    {
        var workflowId = await ResolveWorkflowIdAsync(cancellationToken);
        
        if (workflowId == Guid.Empty)
        {
            return new List<StatusDto>();
        }

        return await FetchStatusesAsync(workflowId, cancellationToken);
    }

    private async Task<Guid> ResolveWorkflowIdAsync(CancellationToken ct)
    {
        // Implicitly resolve WorkspaceId from WorkspaceContext
        var currentWorkspaceId = WorkspaceContext.workspaceId;

        return await (from w in UnitOfWork.Set<Workflow>().AsNoTracking()
                      where w.ProjectWorkspaceId == currentWorkspaceId && w.DeletedAt == null
                      select w.Id)
                     .FirstOrDefaultAsync(ct);
    }

    private async Task<List<StatusDto>> FetchStatusesAsync(Guid workflowId, CancellationToken ct)
    {
        return await (from s in UnitOfWork.Set<Status>().AsNoTracking()
                      where s.WorkflowId == workflowId && s.DeletedAt == null
                      orderby s.CreatedAt
                      select new StatusDto(
                          s.Id,
                          s.Name,
                          s.Color,
                          s.Category
                      ))
                     .ToListAsync(ct);
    }
}
