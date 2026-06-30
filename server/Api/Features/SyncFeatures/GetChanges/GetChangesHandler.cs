namespace Api;

public class GetChangesHandler(SyncQueryService syncQueryService) : IQueryHandler<GetChangesQuery, SyncDeltaBatch>
{
    public async Task<Result<SyncDeltaBatch>> Handle(GetChangesQuery request, CancellationToken cancellationToken)
    {
        var batch = await syncQueryService.GetChangesAsync(request.WorkspaceId, request.Since, cancellationToken);
        return Result<SyncDeltaBatch>.Success(batch);
    }
}
