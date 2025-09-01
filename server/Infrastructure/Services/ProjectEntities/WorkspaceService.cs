using System;
using Application.Features.WorkspaceFeatures.CreateWrokspace;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using SendGrid.Helpers.Errors.Model;

namespace Infrastructure.Services.ProjectEntities;

public class WorkspaceService
{
    private readonly IUnitOfWork _unitOfWork;

    public WorkspaceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken cancellationToken = default)
    {
        var workspace = ProjectWorkspace.Create(command.name, command.description, command.color, command.icon, command.creatorId, command.visibility);

        await _unitOfWork.ProjectWorkspaces.AddAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    #region Update Methods
    public async Task UpdateWorkspaceAsync(Guid id, string? name, string? description, string? color, string? icon, Visibility? visibility, bool? isArchived, bool regenerateJoinCode = false, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.Update(name, description, color, icon, visibility, isArchived, regenerateJoinCode);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task UpdateBasicInfoAsync(Guid id, string name, string? description, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.UpdateBasicInfo(name, description);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task UpdateVisualSettingsAsync(Guid id, string color, string icon, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.UpdateVisualSettings(color, icon);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ChangeVisibilityAsync(Guid id, Visibility visibility, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.ChangeVisibility(visibility);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.Archive();

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task UnarchiveAsync(Guid id, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.Unarchive();

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RegenerateJoinCodeAsync(Guid id, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        workspace.RegenerateJoinCode();

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }
    #endregion

    #region Delete Methods
    public async Task DeleteWorkspaceAsync(Guid id, CancellationToken ct = default)
    {
        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} not found");

        _unitOfWork.ProjectWorkspaces.Remove(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task DeleteWorkspacesAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        if (ids == null || !ids.Any())
            throw new ArgumentException("No workspace IDs provided.", nameof(ids));

        var deletedCount = await _unitOfWork.ProjectWorkspaces.RemoveRangeAsync(ids, ct);

        if (deletedCount != ids.Count())
            throw new NotFoundException("Some workspaces were not found.");
    }
    #endregion

    public async Task AddMembersBulkAsync(Guid workspaceId, IEnumerable<Guid> selectedUserIds, Role role, CancellationToken ct = default)
    {
        if (selectedUserIds == null || !selectedUserIds.Any())
            throw new ArgumentException("No members provided", nameof(selectedUserIds));

        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} not found");

        workspace.AddMembers(selectedUserIds, role);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RemoveMembersBulkAsync(Guid workspaceId, IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds == null || !userIds.Any())
            throw new ArgumentException("No user IDs provided", nameof(userIds));

        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} not found");

        workspace.RemoveMembers(userIds);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }
    public async Task ChangeMemberRolesBulkAsync(Guid workspaceId, IEnumerable<Guid> userIds, Role newRole, CancellationToken ct = default)
    {
        if (userIds == null || !userIds.Any())
            throw new ArgumentException("No user IDs provided", nameof(userIds));

        var workspace = await _unitOfWork.ProjectWorkspaces.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} not found");

        workspace.ChangeMemberRoles(userIds, newRole);

        _unitOfWork.ProjectWorkspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(ct);
    }


}
