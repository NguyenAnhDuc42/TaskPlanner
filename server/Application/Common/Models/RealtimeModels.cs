namespace Application;

public class EntityBatchUpdate
{
    public List<SpaceRecord>? Spaces { get; set; }
    public List<FolderRecord>? Folders { get; set; }
    public List<TaskRecord>? Tasks { get; set; }
    public List<MemberRecord>? Members { get; set; }
    public List<AssigneeRecord>? Assignees { get; set; }
    public List<EntityAccessRecord>? EntityAccess { get; set; }
    public List<WorkspaceRecord>? Workspaces { get; set; }
    public List<StatusRecord>? Statuses { get; set; }
    public List<CommentRecord>? Comments { get; set; }
    public List<DocumentBlockRecord>? DocumentBlocks { get; set; }
    public List<AttachmentRecord>? Attachments { get; set; }
    public List<NotificationRecord>? Notifications { get; set; }

    public bool HasAny => (Spaces?.Count > 0) ||
                          (Folders?.Count > 0) ||
                          (Tasks?.Count > 0) ||
                          (Members?.Count > 0) ||
                          (Assignees?.Count > 0) ||
                          (EntityAccess?.Count > 0) ||
                          (Workspaces?.Count > 0) ||
                          (Statuses?.Count > 0) ||
                          (Comments?.Count > 0) ||
                          (DocumentBlocks?.Count > 0) ||
                          (Attachments?.Count > 0) ||
                          (Notifications?.Count > 0);
}

public class EntityBatchDelete
{
    public List<Guid>? SpaceIds { get; set; }
    public List<Guid>? FolderIds { get; set; }
    public List<Guid>? TaskIds { get; set; }
    public List<Guid>? MemberIds { get; set; }
    public List<Guid>? AssigneeIds { get; set; }
    public List<Guid>? EntityAccessIds { get; set; }
    public List<Guid>? WorkspaceIds { get; set; }
    public List<Guid>? StatusIds { get; set; }
    public List<Guid>? CommentIds { get; set; }
    public List<Guid>? DocumentBlockIds { get; set; }
    public List<Guid>? AttachmentIds { get; set; }
}

public record EntityCreatedResponse(Guid Id, EntityBatchUpdate Batch);
