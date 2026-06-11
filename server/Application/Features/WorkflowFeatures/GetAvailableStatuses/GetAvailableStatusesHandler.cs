using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class GetAvailableStatusesHandler(TaskPlanDbContext db, WorkspaceContext context, HybridCache cache) 
    : IQueryHandler<GetAvailableStatusesQuery, List<StatusRecord>>
{
    public async Task<Result<List<StatusRecord>>> Handle(GetAvailableStatusesQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = context.WorkspaceId;
        var cacheKey = $"AvailableStatuses-{workspaceId}-{request.SpaceId}-{request.FolderId}";
        
        var response = await cache.GetOrCreateAsync(
            cacheKey,
            async cancelToken =>
            {
                var statuses = await WorkflowHelper.GetActiveStatuses(
                    db, 
                    workspaceId, 
                    request.SpaceId, 
                    request.FolderId, 
                    cancelToken);

                return (statuses ?? [])
                    .Select(s => new StatusRecord { Id = s.Id, Name = s.Name, Color = s.Color, Category = s.Category })
                    .ToList();
            },
            tags: [$"Statuses-{context.WorkspaceId}"],
            cancellationToken: cancellationToken
        );

        return Result<List<StatusRecord>>.Success(response);
    }
}


