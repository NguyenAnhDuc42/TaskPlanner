using System;
using Application.Common.Filters;
using Application.Common.Results;
using Application.Features.WorkspaceFeatures.CreateWrokspace;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Infrastructure.Data.Repositories.Extensions;
using Infrastructure.Helper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Errors.Model;
using server.Application.Interfaces;

namespace Infrastructure.Services.ProjectEntities
{
    public class WorkspaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CursorHelper _cursorHelper;
        private readonly ICurrentUserService _currentUserService;

        public WorkspaceService(IUnitOfWork unitOfWork, CursorHelper cursorHelper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cursorHelper = cursorHelper ?? throw new ArgumentNullException(nameof(cursorHelper));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<ProjectWorkspace> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default)
        {
            var workspace = ProjectWorkspace.Create(command.name, command.description, command.color, command.icon, command.creatorId, command.visibility);
            await _unitOfWork.ProjectWorkspaces.AddAsync(workspace, ct);
            return workspace;
        }

        public async Task UpdateWorkspaceAsync(Guid id, string? name, string? description, string? color, string? icon, Visibility? visibility, bool? isArchived, bool regenerateJoinCode = false, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.Update(name, description, color, icon, visibility, isArchived, regenerateJoinCode);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task UpdateBasicInfoAsync(Guid id, string name, string? description, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.UpdateBasicInfo(name, description);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task UpdateVisualSettingsAsync(Guid id, string color, string icon, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.UpdateVisualSettings(color, icon);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task ChangeVisibilityAsync(Guid id, Visibility visibility, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.ChangeVisibility(visibility);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.Archive();
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task UnarchiveAsync(Guid id, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.Unarchive();
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task RegenerateJoinCodeAsync(Guid id, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            workspace.RegenerateJoinCode();
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task DeleteWorkspaceAsync(Guid id, CancellationToken ct = default)
        {
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct) ?? throw new NotFoundException($"Workspace {id} not found");
            _unitOfWork.ProjectWorkspaces.Remove(workspace);
        }

        public async Task DeleteWorkspacesAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            if (ids == null || !ids.Any()) throw new ArgumentException("No workspace IDs provided.", nameof(ids));
            var deletedCount = await _unitOfWork.ProjectWorkspaces.RemoveRangeAsync(ids, ct);
            if (deletedCount != ids.Count()) throw new NotFoundException("Some workspaces were not found.");
        }

        public async Task AddMembersBulkAsync(Guid workspaceId, IEnumerable<Guid> selectedUserIds, Role role, CancellationToken ct = default)
        {
            if (selectedUserIds == null || !selectedUserIds.Any()) throw new ArgumentException("No members provided", nameof(selectedUserIds));
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.AddMembers(selectedUserIds, role);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task RemoveMembersBulkAsync(Guid workspaceId, IEnumerable<Guid> userIds, CancellationToken ct = default)
        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No user IDs provided", nameof(userIds));
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.RemoveMembers(userIds);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }

        public async Task ChangeMemberRolesBulkAsync(Guid workspaceId, IEnumerable<Guid> userIds, Role newRole, CancellationToken ct = default)

        {
            if (userIds == null || !userIds.Any()) throw new ArgumentException("No user IDs provided", nameof(userIds));
            var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct) ?? throw new NotFoundException($"Workspace {workspaceId} not found");
            workspace.ChangeMemberRoles(userIds, newRole);
            _unitOfWork.ProjectWorkspaces.Update(workspace);
        }
        public async Task<PagedResult<ProjectWorkspace>> GetWorkspacesAsync(WorkspaceFilter filter, CursorPaginationRequest pagination, CancellationToken cancellationToken = default)
        {

            var currentUserId = _currentUserService.CurrentUserId();
            var query = _unitOfWork.ProjectWorkspaces
            .AsQueryable()
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
    }
}
