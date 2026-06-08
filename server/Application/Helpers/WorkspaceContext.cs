using Microsoft.AspNetCore.Http;
namespace Application;

public class WorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkspaceMember CurrentMember { get; set; } = null!;

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

    public Result<Guid> TryGetWorkspaceId()
    {
        var id = _httpContextAccessor.HttpContext?.Items["WorkspaceId"] as Guid?;

        if (!id.HasValue || id == Guid.Empty)
            return Result<Guid>.Failure(Error.Failure("Workspace.NotFound", "Workspace ID not found in context."));

        return Result<Guid>.Success(id.Value);
    }
}


