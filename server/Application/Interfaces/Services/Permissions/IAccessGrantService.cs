using Domain.Enums.RelationShip;

namespace Application.Interfaces.Services.Permissions;

public interface IAccessGrantService
{
    Task GrantAccess(
        Guid workspaceMemberId,
        EntityLayerType entityLayer,
        Guid entityId,
        AccessLevel accessLevel,
        Guid grantedByUserId,
        CancellationToken ct);

    Task RevokeAccess(
        Guid workspaceMemberId,
        EntityLayerType entityLayer,
        Guid entityId,
        CancellationToken ct);
}
