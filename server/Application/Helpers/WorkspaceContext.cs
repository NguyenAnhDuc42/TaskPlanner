using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Data;
using Application.Common.Results;
using Domain.Entities.Relationship;
using server.Application.Interfaces;

namespace Application.Helpers;

public class WorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDataBase _db;
    private Guid? _workspaceMemberId;

    public WorkspaceContext(IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService, IDataBase db)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
        _db = db;
    }

    public Result<Guid> TryGetWorkspaceId()
    {
        var id = _httpContextAccessor.HttpContext?.Items["WorkspaceId"] as Guid?;
        
        if (!id.HasValue || id == Guid.Empty)
            return Error.Failure("Workspace.NotFound", "Workspace ID not found in context.");

        return id.Value;
    }

    public async Task<Result<Guid>> GetCurrentMemberIdAsync(CancellationToken ct = default)
    {
        if (_workspaceMemberId.HasValue)
            return _workspaceMemberId.Value;

        var workspaceIdResult = TryGetWorkspaceId();
        if (workspaceIdResult.IsFailure) return workspaceIdResult;

        var userId = _currentUserService.CurrentUserId();
        var workspaceId = workspaceIdResult.Value;

        var memberId = await _db.WorkspaceMembers
            .AsNoTracking()
            .Where(wm => wm.UserId == userId && wm.ProjectWorkspaceId == workspaceId)
            .Select(wm => wm.Id)
            .FirstOrDefaultAsync(ct);

        if (memberId == Guid.Empty)
            return Error.Unauthorized("Workspace.NotMember", $"User is not a member of workspace {workspaceId}");

        _workspaceMemberId = memberId;
        return _workspaceMemberId.Value;
    }

    [Obsolete("Use TryGetWorkspaceId instead to handle failures gracefully.")]
    public Guid workspaceId
    {
        get
        {
            var result = TryGetWorkspaceId();
            if (result.IsFailure)
            {
                throw new InvalidOperationException(result.Error.Description);
            }
            return result.Value;
        }
    }

    [Obsolete("Use GetCurrentMemberIdAsync instead to handle unauthorized access gracefully.")]
    public async Task<Guid> GetWorkspaceMemberIdAsync(CancellationToken ct = default)
    {
        var result = await GetCurrentMemberIdAsync(ct);
        if (result.IsFailure)
        {
            if (result.Error == Error.Unauthorized())
            {
                throw new UnauthorizedAccessException(result.Error.Description);
            }
            throw new InvalidOperationException(result.Error.Description);
        }
        return result.Value;
    }
}
