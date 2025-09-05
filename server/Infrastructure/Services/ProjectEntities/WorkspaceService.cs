using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Filters;
using Application.Common.Results;
using Application.Features.WorkspaceFeatures.CreateWrokspace;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Services.UsageChecker;
using Infrastructure.Data.Repositories.Extensions;
using Infrastructure.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Errors.Model;
using server.Application.Interfaces;

namespace Infrastructure.Services.ProjectEntities
{
    public class WorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CursorHelper _cursorHelper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPermissionService _permissionService;
        private readonly HybridCache _cache;
        private readonly ILogger<WorkspaceService> _logger;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public WorkspaceService(
            IUnitOfWork unitOfWork,
            CursorHelper cursorHelper,
            ICurrentUserService currentUserService,
            IPermissionService permissionService,
            HybridCache cache,
            ILogger<WorkspaceService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cursorHelper = cursorHelper ?? throw new ArgumentNullException(nameof(cursorHelper));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProjectWorkspace> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
        {
            var workspace = ProjectWorkspace.Create(command.name, command.description, command.color, command.icon, command.creatorId, command.visibility);
            await _unitOfWork.ProjectWorkspaces.AddAsync(workspace, ct);

            _logger.LogInformation("Workspace {WorkspaceId} created by {CreatorId}", workspace.Id, command.creatorId);

            // Note: consider invalidating any cached workspace lists here if you add list-level caching.
            return workspace;
        }

        public async Task UpdateWorkspaceAsync(Guid id, string? name, string? description, string? color, string? icon, Visibility? visibility, bool? isArchived, bool regenerateJoinCode = false, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();

            // enforce appropriate permission for workspace updates. Adjust permission mapping as business rules require.
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.Update(name, description, color, icon, visibility, isArchived, regenerateJoinCode);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} updated by {UserId}", id, currentUserId);

            // Invalidate workspace cache
            var cacheKey = $"workspace_{id}";
            try
            {
                await _cache.RemoveAsync(cacheKey, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove cache for workspace {WorkspaceId}", id);
            }
        }

        public async Task UpdateBasicInfoAsync(Guid id, string name, string? description, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.UpdateBasicInfo(name, description);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} basic info updated by {UserId}", id, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task UpdateVisualSettingsAsync(Guid id, string color, string icon, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.UpdateVisualSettings(color, icon);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} visual settings updated by {UserId}", id, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task ChangeVisibilityAsync(Guid id, Visibility visibility, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.ChangeVisibility(visibility);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} visibility changed to {Visibility} by {UserId}", id, visibility, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.Archive();
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} archived by {UserId}", id, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task UnarchiveAsync(Guid id, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.Unarchive();
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} unarchived by {UserId}", id, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task RegenerateJoinCodeAsync(Guid id, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.RegenerateJoinCode();
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} join code regenerated by {UserId}", id, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task DeleteWorkspaceAsync(Guid id, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            _unitOfWork.ProjectWorkspaces.Remove(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} deleted by {UserId}", id, currentUserId);
            await _cache.RemoveAsync($"workspace_{id}", ct);
        }

        public async Task DeleteWorkspacesAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            if (ids == null || !ids.Any()) throw new ArgumentException("No workspace IDs provided.", nameof(ids));
            var currentUserId = _currentUserService.CurrentUserId();

            // Require admin permission on each workspace. Consider optimizing with a bulk permission check.
            foreach (var id in ids)
            {
                await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.Workspace_Admin, ct);
            }

            var deletedCount = await _unitOfWork.ProjectWorkspaces.RemoveRangeAsync(ids, ct);
            if (deletedCount != ids.Count()) throw new NotFoundException("Some workspaces were not found.");

            _logger.LogInformation("Workspaces deleted by {UserId}: {WorkspaceIds}", currentUserId, string.Join(',', ids));

            foreach (var id in ids)
            {
                await _cache.RemoveAsync($"workspace_{id}", ct);
            }
        }

        public async Task AddMembersBulkAsync(Guid workspaceId, IEnumerable<Guid> selectedUserIds, Role role, CancellationToken ct = default)
        {
            if (selectedUserIds == null || !selectedUserIds.Any()) throw new ArgumentException("No members provided", nameof(selectedUserIds));
            var currentUserId = _currentUserService.CurrentUserId();

            // Member management requires member-admin permission (tweak as needed).
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Member_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.AddMembers(selectedUserIds, role);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Members added to workspace {WorkspaceId} by {UserId}", workspaceId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task RemoveMembersBulkAsync(Guid workspaceId, IEnumerable<Guid> userIds, CancellationToken ct = default)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No user IDs provided", nameof(userIds));
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Member_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.RemoveMembers(userIds);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Members removed from workspace {WorkspaceId} by {UserId}", workspaceId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task ChangeMemberRolesBulkAsync(Guid workspaceId, IEnumerable<Guid> userIds, Role newRole, CancellationToken ct = default)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No user IDs provided", nameof(userIds));
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Member_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.ChangeMemberRoles(userIds, newRole);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Member roles changed in workspace {WorkspaceId} by {UserId}", workspaceId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task<PagedResult<ProjectWorkspace>> GetWorkspacesAsync(WorkspaceFilter filter, CursorPaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            _logger.LogDebug("Retrieving workspaces list for user {UserId} with filter {@Filter} and pagination {@Pagination}", currentUserId, filter, pagination);

            var query = _unitOfWork.ProjectWorkspaces
                .AsQueryable().AsNoTracking()
                .ForUser(currentUserId)
                .ApplyFilter(filter, currentUserId)
                .ApplySorting(pagination.SortBy, pagination.Direction)
                .ApplyCursor(pagination, _cursorHelper);

            var items = await query
                .Take(pagination.PageSize + 1)
                .ToListAsync(cancellationToken);

            var hasNextPage = items.Count > pagination.PageSize;
            var pagedItems = hasNextPage ? items.Take(pagination.PageSize) : items;
            var nextCursor = hasNextPage ? pagedItems.BuildNextCursor(pagination, _cursorHelper) : null;
            return new PagedResult<ProjectWorkspace>(
                Items: pagedItems,
                NextCursor: nextCursor,
                HasNextPage: hasNextPage
            );
        }

        public async Task<ProjectWorkspace> GetWorkspaceByIdAsync(Guid id, CancellationToken ct)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            var cacheKey = $"workspace_{id}";

            _logger.LogDebug("Fetching workspace {WorkspaceId} for user {UserId}", id, currentUserId);

            // Cache workspace entity; still enforce per-user permission after retrieval.
            var workspace = await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                var w = await _unitOfWork.ProjectWorkspaces.Query.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, token);
                if (w == null)
                    throw new NotFoundException($"Workspace {id} not found or you do not have access.");
                return w;
            }, new HybridCacheEntryOptions { Expiration = CacheDuration });

            await _permissionService.EnsurePermissionAsync(currentUserId, id, Permission.View_Workspace, ct);

            return workspace;
        }

        public async Task<ProjectWorkspace> GetWorkspaceByJoinCodeAsync(string joinCode, CancellationToken ct)
        {
            var cacheKey = $"workspace_joincode_{joinCode}";
            _logger.LogDebug("Lookup workspace by join code {JoinCode}", joinCode);

            var workspace = await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                var w = await _unitOfWork.ProjectWorkspaces.Query.AsNoTracking().FirstOrDefaultAsync(w => w.JoinCode == joinCode, token);
                if (w == null)
                    throw new NotFoundException($"Workspace with join code {joinCode} not found.");
                return w;
            }, new HybridCacheEntryOptions { Expiration = CacheDuration });

            switch (workspace.Visibility)
            {
                case Visibility.Private:
                    _logger.LogWarning("Attempt to join private workspace by join code {JoinCode}", joinCode);
                    throw new UnauthorizedAccessException("Cannot join a private workspace via join code.");
                case Visibility.Public:
                case Visibility.Restricted:
                    return workspace;
                default:
                    throw new InvalidOperationException("Invalid workspace visibility.");
            }
        }

        public async Task AddMembersAsync(IEnumerable<Guid> userIds, Guid workspaceId, Role role, CancellationToken ct = default)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No members provided", nameof(userIds));
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Member_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.AddMembers(userIds, role);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Members added to workspace {WorkspaceId} by {UserId}", workspaceId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task RemoveMembersAsync(IEnumerable<Guid> userIds, Guid workspaceId, CancellationToken ct = default)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No user IDs provided", nameof(userIds));
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Member_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.RemoveMembers(userIds);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Members removed from workspace {WorkspaceId} by {UserId}", workspaceId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task ChangeMemberRolesAsync(IEnumerable<Guid> userIds, Guid workspaceId, Role newRole, CancellationToken ct = default)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No user IDs provided", nameof(userIds));
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Member_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.ChangeMemberRoles(userIds, newRole);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Member roles changed in workspace {WorkspaceId} by {UserId}", workspaceId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task TransferOwnershipAsync(Guid workspaceId, Guid newOwnerId, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Owner_Permissions, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.TransferOwnership(newOwnerId);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Workspace {WorkspaceId} ownership transferred to {NewOwnerId} by {UserId}", workspaceId, newOwnerId, currentUserId);
            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task CreateStatusAsync(Guid workspaceId, string name, string color, bool isDefaultStatus, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.Query.Include(w => w.Statuses).FirstOrDefaultAsync(w => w.Id == workspaceId, ct)
                ?? throw new NotFoundException($"Workspace {workspaceId} not found");

            // assign new key at the end
            var maxKey = workspace.Statuses.Any()
                ? workspace.Statuses.Max(s => s.OrderKey)
                : 0;

            var nextKey = maxKey + 10000;

            workspace.CreateStatus(name, color, nextKey, isDefaultStatus);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Status created in workspace {WorkspaceId} by {UserId} at orderKey {OrderKey}",
                workspaceId, currentUserId, nextKey);

            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        public async Task UpdateStatusAsync(Guid workspaceId, Guid statusId, string? name, string? color, bool? isDefaultStatus = null, long? orderKey = null, CancellationToken ct = default)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.Query.Include(w => w.Statuses).FirstOrDefaultAsync(w => w.Id == workspaceId, ct)
                 ?? throw new NotFoundException($"Workspace {workspaceId} not found");

            var updateName = name ?? workspace.Name;
            var updateColor = color ?? workspace.Color;

            workspace.UpdateStatus(statusId, updateName, updateColor, orderKey, isDefaultStatus);
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Status {StatusId} updated in workspace {WorkspaceId} by {UserId}",
                statusId, workspaceId, currentUserId);

            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }

        // Intentionally left unfinished per request - keep method placeholder
        public async Task ReorderStatusesAsync(Guid workspaceId, List<Guid> orderedStatusIds, CancellationToken ct)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.Query.Include(w => w.Statuses).FirstOrDefaultAsync(w => w.Id == workspaceId, ct)
                ?? throw new NotFoundException($"Workspace {workspaceId} not found");

            // validate: all ids must belong to workspace
            var existingIds = workspace.Statuses.Select(s => s.Id).ToHashSet();
            if (!orderedStatusIds.All(existingIds.Contains))
                throw new InvalidOperationException("One or more statusIds do not belong to this workspace.");

            // reassign order keys with spacing
            long step = 10000;
            for (int i = 0; i < orderedStatusIds.Count; i++)
            {
                var status = workspace.Statuses.First(s => s.Id == orderedStatusIds[i]);
                var newKey = (i + 1) * step;
                status.UpdateOrderKey(newKey);
            }

            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Statuses reordered in workspace {WorkspaceId} by {UserId}",
                workspaceId, currentUserId);

            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }
        public async Task DeleteStatusAsync(Guid workspaceId, Guid statusId, IStatusUsageCheker usageChecker, CancellationToken ct)
        {
            var currentUserId = _currentUserService.CurrentUserId();
            await _permissionService.EnsurePermissionAsync(currentUserId, workspaceId, Permission.Workspace_Admin, ct);

            var workspace = await _unitOfWork.ProjectWorkspaces.Query.Include(w => w.Statuses).FirstOrDefaultAsync(w => w.Id == workspaceId, ct)
                ?? throw new NotFoundException($"Workspace {workspaceId} not found");

            var status = workspace.Statuses.FirstOrDefault(s => s.Id == statusId);
            if (status == null)
                throw new NotFoundException($"Status {statusId} not found in workspace {workspaceId}");

            // check usage
            var isUsed = await usageChecker.IsStatusInUseAsync(statusId, ct);
            if (isUsed)
                throw new InvalidOperationException($"Status {statusId} is in use and cannot be deleted.");

            workspace.RemoveStatus(statusId); // domain method you should expose
            _unitOfWork.ProjectWorkspaces.Update(workspace);

            _logger.LogInformation("Status {StatusId} deleted in workspace {WorkspaceId} by {UserId}",
                statusId, workspaceId, currentUserId);

            await _cache.RemoveAsync($"workspace_{workspaceId}", ct);
        }
        F

        public async Task<Status?> GetStatusByIdAsync(Guid workspaceId, Guid statusId, CancellationToken ct)
        {

        }

        public async Task<IEnumerable<Status>> GetAllStatusesAsync(Guid workspaceId, CancellationToken ct)
        {

        }
        public async Task AddTagAsync(Guid workspaceId, string tag, CancellationToken ct)
        {

        }
        public async Task RemoveTagAsync(Guid workspaceId, string tag, CancellationToken ct)
        {

        }

        partial async IEnumerable<Tag> GetTagsAsync(Guid workspaceId, CancellationToken ct)
        {

        }
    }
}
