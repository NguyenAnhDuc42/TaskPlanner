using Domain.Common;

namespace Domain.Events.Space;

public record SpaceCreatedEvent(Guid WorkspaceId, Guid SpaceId, Guid UserId) : BaseDomainEvent;
