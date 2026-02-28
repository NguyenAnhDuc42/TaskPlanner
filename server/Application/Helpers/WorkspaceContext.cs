using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Repositories;
using server.Application.Interfaces;
using Domain.Entities.Relationship;

namespace Application.Helpers;

public class WorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private Guid? _workspaceMemberId;

    public WorkspaceContext(
        IHttpContextAccessor httpContextAccessor, 
        ICurrentUserService currentUserService, 
        IUnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public Guid workspaceId
    {
        get
        {
           var id = _httpContextAccessor.HttpContext?.Items["WorkspaceId"] as Guid?;
            
            if (!id.HasValue)
            {
                throw new InvalidOperationException(
                    "Workspace ID not found. Ensure the request includes X-Workspace-Id header or workspaceId query parameter."
                );
            }

            return id.Value;
        }
    }

    public async Task<Guid> GetWorkspaceMemberIdAsync(CancellationToken ct = default)
    {
        if (_workspaceMemberId.HasValue)
            return _workspaceMemberId.Value;

        var userId = _currentUserService.CurrentUserId();
        var currentWorkspaceId = workspaceId;

        var memberId = await _unitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => wm.UserId == userId && wm.ProjectWorkspaceId == currentWorkspaceId)
            .Select(wm => wm.Id)
            .FirstOrDefaultAsync(ct);

        if (memberId == Guid.Empty)
        {
            throw new UnauthorizedAccessException($"User {userId} is not a member of workspace {currentWorkspaceId}");
        }

        _workspaceMemberId = memberId;
        return _workspaceMemberId.Value;
    }
}
