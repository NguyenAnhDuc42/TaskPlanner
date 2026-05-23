using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class GetAvailableStatusesHandler(TaskPlanDbContext db, WorkspaceContext context, HybridCache cache) 
    : IQueryHandler<GetAvailableStatusesQuery, List<StatusRecord>>
{
    public async Task<Result<List<StatusRecord>>> Handle(GetAvailableStatusesQuery request, CancellationToken ct)
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

                return (statuses ?? [])
                    .Select(s => new StatusRecord { Id = s.Id, Name = s.Name, Color = s.Color, Category = s.Category })
                    .ToList();
            },
            tags: [$"Statuses-{context.workspaceId}"],
            cancellationToken: ct
        );

        return Result<List<StatusRecord>>.Success(response);
    }
}


