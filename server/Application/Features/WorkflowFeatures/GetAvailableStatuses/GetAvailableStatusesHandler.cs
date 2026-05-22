using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Features.WorkflowFeatures;

public class GetAvailableStatusesHandler(IDataBase db, WorkspaceContext context, HybridCache cache) 
    : IQueryHandler<GetAvailableStatusesQuery, List<StatusResponse>>
{
    public async Task<Result<List<StatusResponse>>> Handle(GetAvailableStatusesQuery request, CancellationToken ct)
    {
        var cacheKey = $"AvailableStatuses-{context.workspaceId}-{request.SpaceId}-{request.FolderId}";
        
        var response = await cache.GetOrCreateAsync(
            cacheKey,
            async cancelToken =>
            {
                var statuses = await WorkflowHelper.GetActiveStatuses(
                    db, 
                    context.workspaceId, 
                    request.SpaceId, 
                    request.FolderId, 
                    cancelToken);

                return statuses
                    .Select(s => new StatusResponse(s.Id, s.Name, s.Color, s.Category))
                    .ToList();
            },
            tags: [$"Statuses-{context.workspaceId}"],
            cancellationToken: ct
        );

        return Result<List<StatusResponse>>.Success(response);
    }
}
