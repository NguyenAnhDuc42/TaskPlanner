using Application.Common;
using Application.Contract.UserContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public class GetMembersHandler : BaseQueryHandler, IRequestHandler<GetMembersQuery, List<MemberDto>>
{
    private readonly HybridCache _cache;

    public GetMembersHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper,
        HybridCache cache)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext, cursorHelper)
    {
        _cache = cache;
    }

    public async Task<List<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var workspace = await QueryNoTracking<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        // Check access
        await RequirePermissionAsync(workspace, EntityType.WorkspaceMember, PermissionAction.View, cancellationToken);

        // Get from cache or load from DB
        return await _cache.GetOrCreateAsync(
            CacheKeys.WorkspaceMembers(request.WorkspaceId),
            async ct => await LoadMembersFromDb(request.WorkspaceId, ct),
            cancellationToken: cancellationToken
        ) ?? new List<MemberDto>();
    }

    private async Task<List<MemberDto>> LoadMembersFromDb(Guid workspaceId, CancellationToken ct)
    {
        return await QueryNoTracking<WorkspaceMember>()
            .Where(wm => wm.ProjectWorkspaceId == workspaceId && wm.DeletedAt == null)
            .Select(wm => new MemberDto
            {
                Id = wm.UserId,
                Role = wm.Role
            })
            .ToListAsync(ct);
    }
}
