namespace Api;

public record GetBootstrapQuery(Guid WorkspaceId) : IQueryRequest<BootstrapResult>, IAuthorizedWorkspaceRequest;

public record BootstrapResult(
    long LastSyncId,
    int DatabaseVersion,
    List<TaskRecord> Tasks,
    List<SpaceRecord> Spaces,
    List<FolderRecord> Folders,
    List<StatusRecord> Statuses,
    List<AssigneeRecord> Assignees,
    List<FavoriteRecord> Favorites,
    List<MemberRecord> Members
);
