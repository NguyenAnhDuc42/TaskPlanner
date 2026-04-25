using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;

namespace Application.Features.WorkflowFeatures;

public class GetAvailableStatusesHandler(IDataBase db, WorkspaceContext context) 
    : IQueryHandler<GetAvailableStatusesQuery, List<StatusResponse>>
{
    public async Task<Result<List<StatusResponse>>> Handle(GetAvailableStatusesQuery request, CancellationToken ct)
    {
        var statuses = await WorkflowHelper.GetActiveStatuses(
            db, 
            context.workspaceId, 
            request.SpaceId, 
            request.FolderId, 
            ct);

        var response = statuses
            .Select(s => new StatusResponse(s.Id, s.Name, s.Color, s.Category))
            .ToList();

        return Result<List<StatusResponse>>.Success(response);
    }
}
