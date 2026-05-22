namespace Application;

public record EntityAccessBatchCommand(Guid SpaceId, IEnumerable<EntityAccessRowsValue> Rows) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record EntityAccessRowsValue(Guid MemberId, AccessLevel AccessLevel,RowAction Action);



