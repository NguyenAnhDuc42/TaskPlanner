using Domain.Common;

namespace Domain.Events.Folder;

public record FolderCreatedEvent(Guid WorkspaceId, Guid SpaceId, Guid FolderId, Guid UserId) : BaseDomainEvent;

