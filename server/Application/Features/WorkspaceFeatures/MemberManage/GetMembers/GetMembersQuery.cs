namespace Application;

public record class GetMembersQuery(
    CursorPaginationRequest pagination, 
    Guid WorkspaceId, 
    GetMembersFilter filter
) : IQueryRequest<PagedResult<MemberRecord>>, IAuthorizedWorkspaceRequest;

public record class GetMembersFilter(
    string? Name,
    string? Email,
    Guid? SpaceId,
    Guid? TaskId,
    Role? Role
);

