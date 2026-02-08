

using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Application.Features;

public abstract class BaseCommandHandler : BaseFeatureHandler
{
    protected BaseCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    /// <summary>
    /// Validates that all provided user IDs are members of the current workspace.
    /// Returns the list of valid user IDs.
    /// Throws ValidationException if any user is not a workspace member.
    /// </summary>
    protected new async Task<List<Guid>> ValidateWorkspaceMembers(List<Guid> userIds, CancellationToken ct)
    {
        if (userIds == null || !userIds.Any())
            return new List<Guid>();

        var validMembers = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => userIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .Select(wm => wm.UserId)
            .ToListAsync(ct);

        if (validMembers.Count != userIds.Count)
        {
            var invalidIds = userIds.Except(validMembers).ToList();
            throw new ValidationException($"One or more users are not workspace members. Invalid user IDs: {string.Join(", ", invalidIds)}");
        }

        return validMembers;
    }
}
