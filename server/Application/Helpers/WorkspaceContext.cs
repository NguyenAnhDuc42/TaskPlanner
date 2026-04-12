using Domain.Entities.Relationship;

namespace Application.Helpers;

public class WorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkspaceMember CurrentMember { get; set; } = null!;

    public Guid workspaceId => TryGetWorkspaceId().Value;

    public WorkspaceContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Result<Guid> TryGetWorkspaceId()
    {
        var id = _httpContextAccessor.HttpContext?.Items["WorkspaceId"] as Guid?;
        
        if (!id.HasValue || id == Guid.Empty)
            return Result<Guid>.Failure(Error.Failure("Workspace.NotFound", "Workspace ID not found in context."));

        return id.Value;
    }
}
