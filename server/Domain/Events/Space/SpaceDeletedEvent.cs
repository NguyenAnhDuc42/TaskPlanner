using Domain.Common;

namespace Domain.Events.Space;

public record SpaceDeletedEvent(Guid WorkspaceId, Guid SpaceId, Guid UserId) : BaseDomainEvent;
