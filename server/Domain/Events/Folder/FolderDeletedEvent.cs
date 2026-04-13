using Domain.Common;

namespace Domain.Events.Folder;

public record FolderDeletedEvent(Guid WorkspaceId, Guid SpaceId, Guid FolderId, Guid UserId) : BaseDomainEvent;
