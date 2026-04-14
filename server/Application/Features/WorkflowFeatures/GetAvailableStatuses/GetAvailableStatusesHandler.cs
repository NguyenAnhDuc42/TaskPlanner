using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.WorkflowFeatures.Common;
using Application.Helpers;
using Application.Interfaces.Data;

namespace Application.Features.WorkflowFeatures.GetAvailableStatuses;

public class GetAvailableStatusesHandler(IDataBase db, WorkspaceContext context) 
    : IQueryHandler<GetAvailableStatusesQuery, List<StatusResponse>>
{
    public async Task<Result<List<StatusResponse>>> Handle(GetAvailableStatusesQuery request, CancellationToken ct)
    {
        // 1. Resolve statuses via bubbling logic in WorkflowHelper
        var statuses = await WorkflowHelper.GetActiveStatuses(
            db, 
            context.workspaceId, 
            request.SpaceId, 
            request.FolderId, 
            ct);

        // 2. Map to Response
        var response = statuses
            .Select(s => new StatusResponse(s.Id, s.Name, s.Color, s.Category))
            .ToList();

        return Result<List<StatusResponse>>.Success(response);
    }
}
