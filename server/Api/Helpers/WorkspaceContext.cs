using Microsoft.AspNetCore.Http;
namespace Api;

// Scoped (one instance per request) — CurrentMember is a plain instance field. The ONLY writer
// is WorkspaceContextMiddleware, which resolves membership once per request (the blanket "is
// this user allowed to touch this workspace at all" gate) and sets it here on success.
// PipelineDecorator reads it, it doesn't re-resolve it — one query per request, not two.
// Trade-off, made deliberately: this couples the decorator to that specific middleware having
// run first. If some future invocation path (a hub, a background job, a direct dispatcher call)
// ever needs IAuthorizedWorkspaceRequest handling without going through this middleware,
// CurrentMember won't be set and PipelineDecorator's own check below will fail gracefully
// (Workspace.Required) rather than silently doing its own query — that's an intentional decision
// to keep this to a single query, not an oversight.
public class WorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private WorkspaceMember? _currentMember;

    public bool HasCurrentMember => _currentMember != null;

    public WorkspaceMember CurrentMember
    {
        get
        {
            if (_currentMember == null)
                throw new InvalidOperationException("CurrentMember is not set in the current context.");
            return _currentMember;
        }
    }

    public void SetCurrentMember(WorkspaceMember member) => _currentMember = member;

    public Guid WorkspaceId
    {
        get
        {
            var result = TryGetWorkspaceId();
            if (result.IsFailure)
                throw new InvalidOperationException("WorkspaceId is not set in the current context.");
            return result.Value!;
        }
    }

    public WorkspaceContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // WorkspaceId extraction (header/route/query) is inherently an HTTP-request concept — stays
    // backed by HttpContext.Items (the same key IdempotencyMiddleware also reads).
    public Result<Guid> TryGetWorkspaceId()
    {
        var id = _httpContextAccessor.HttpContext?.Items[HttpContextItemKeys.WorkspaceId] as Guid?;

        if (!id.HasValue || id == Guid.Empty)
            return Result<Guid>.Failure(Error.Failure("Workspace.NotFound", "Workspace ID not found in context."));

        return Result<Guid>.Success(id.Value);
    }
}
