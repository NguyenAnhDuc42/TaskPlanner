using System;
using Domain.Enums;

namespace Application.Interfaces.Services;

public interface IWorkspacePermissionService
{
    Task<Role> CheckForUser(Guid workspaceId, Guid userId,CancellationToken ct = default);
}
