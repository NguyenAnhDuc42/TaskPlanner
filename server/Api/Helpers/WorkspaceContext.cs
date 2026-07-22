using Microsoft.AspNetCore.Http;
namespace Api;

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

    public Result<Guid> TryGetWorkspaceId()
    {
        var id = _httpContextAccessor.HttpContext?.Items[HttpContextItemKeys.WorkspaceId] as Guid?;

        if (!id.HasValue || id == Guid.Empty)
            return Result<Guid>.Failure(Error.Failure("Workspace.NotFound", "Workspace ID not found in context."));

        return Result<Guid>.Success(id.Value);
    }
}
