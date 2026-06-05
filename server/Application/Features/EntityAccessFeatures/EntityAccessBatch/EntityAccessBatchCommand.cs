namespace Application;

public record EntityAccessBatchCommand(Guid SpaceId, IEnumerable<EntityAccessRowsValue> Rows) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record EntityAccessRowsValue(Guid? Id, Guid MemberId, AccessLevel AccessLevel, RowAction Action);



