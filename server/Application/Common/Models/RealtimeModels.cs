namespace Application;

public class EntityBatchUpdate
{
    public List<SpaceRecord>? Spaces { get; set; }
    public List<FolderRecord>? Folders { get; set; }
    public List<TaskRecord>? Tasks { get; set; }
    public List<MemberRecord>? Members { get; set; }
    public List<AssigneeRecord>? Assignees { get; set; }
    public List<EntityAccessRecord>? EntityAccess { get; set; }

    public bool HasAny => (Spaces?.Count > 0) || 
                          (Folders?.Count > 0) || 
                          (Tasks?.Count > 0) || 
                          (Members?.Count > 0) ||
                          (Assignees?.Count > 0) ||
                          (EntityAccess?.Count > 0);
}

public class EntityBatchDelete
{
    public List<Guid>? SpaceIds { get; set; }
    public List<Guid>? FolderIds { get; set; }
    public List<Guid>? TaskIds { get; set; }
    public List<Guid>? MemberIds { get; set; }
    public List<Guid>? AssigneeIds { get; set; }
    public List<Guid>? EntityAccessIds { get; set; }
}
