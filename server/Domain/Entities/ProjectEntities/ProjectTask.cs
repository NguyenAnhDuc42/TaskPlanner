using Domain.Common.Interfaces;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;

namespace Domain.Entities.ProjectEntities;

public class ProjectTask : Aggregate
{
    private const int MAX_BATCH_SIZE = 200; // Safety limit for subtask batch ops

    public Guid ProjectListId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CreatorId { get; private set; }

    public Guid? StatusId { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsArchived { get; private set; }

    public Priority Priority { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimateSeconds { get; private set; }
    public long? OrderKey { get; private set; }
    public Visibility Visibility { get; private set; }

    private readonly List<UserProjectTask> _assignees = new();
    public IReadOnlyCollection<UserProjectTask> Assignees => _assignees.AsReadOnly();

    private readonly List<ProjectTaskTag> _tags = new();
    public IReadOnlyCollection<ProjectTaskTag> Tags => _tags.AsReadOnly();

    private readonly List<Comment> _comments = new();
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private readonly List<Attachment> _attachments = new();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    private readonly List<TimeLog> _timeLogs = new();
    public IReadOnlyCollection<TimeLog> TimeLogs => _timeLogs.AsReadOnly();

    private readonly List<Checklist> _checklists = new();
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();

    private ProjectTask() { } // EF Core

    internal ProjectTask(Guid id, Guid projectListId, string name, string? description,
        Priority priority, DateTimeOffset? startDate, DateTimeOffset? dueDate, Visibility visibility,
        long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectListId = projectListId;
        Name = name;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        Visibility = visibility;
        OrderKey = orderKey;
        CreatorId = creatorId;

        IsCompleted = false;
        IsArchived = false;
        StatusId = null;
        StoryPoints = null;
        TimeEstimateSeconds = null;
    }

    // === SELF MANAGEMENT ===

    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name && Description == description) return;

        ValidateBasicInfo(name, description);

        var oldName = Name;
        var oldDescription = Description;
        Name = name;
        Description = description;
        UpdateTimestamp();
    }

    public void UpdateSchedule(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (StartDate == startDate && DueDate == dueDate) return;

        ValidateSchedule(startDate, dueDate);

        var oldStartDate = StartDate;
        var oldDueDate = DueDate;
        StartDate = startDate;
        DueDate = dueDate;
        UpdateTimestamp();
    }

    public void ChangePriority(Priority newPriority)
    {
        if (Priority == newPriority) return;

        var oldPriority = Priority;
        Priority = newPriority;
        UpdateTimestamp();
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
    }

    public void UpdateStatus(Guid statusId, bool isCompletedStatus)
    {
        if (StatusId == statusId && IsCompleted == isCompletedStatus) return;

        var oldStatusId = StatusId;
        var wasCompleted = IsCompleted;

        StatusId = statusId;
        IsCompleted = isCompletedStatus;

        UpdateTimestamp();

    }

    public void SetEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        ValidateEstimation(storyPoints, timeEstimateSeconds);

        if (StoryPoints == storyPoints && TimeEstimateSeconds == timeEstimateSeconds) return;

        var oldStoryPoints = StoryPoints;
        var oldTimeEstimate = TimeEstimateSeconds;
        StoryPoints = storyPoints;
        TimeEstimateSeconds = timeEstimateSeconds;
        UpdateTimestamp();
    }

    internal void UpdateOrderKey(long newOrderKey)
    {
        if (OrderKey == newOrderKey) return;

        OrderKey = newOrderKey;
        UpdateTimestamp();
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
    }

    // === ASSIGNMENT ===

    public void AssignUser(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        if (_assignees.Any(a => a.UserId == userId))
            throw new InvalidOperationException("User is already assigned to this task.");

        var assignment = UserProjectTask.Create(userId, Id);
        _assignees.Add(assignment);
        UpdateTimestamp();
    }

    public void UnassignUser(Guid userId)
    {
        var assignment = _assignees.FirstOrDefault(a => a.UserId == userId);
        if (assignment == null) return;

        _assignees.Remove(assignment);
        UpdateTimestamp();
    }

    public bool IsAssignedTo(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));
        return _assignees.Any(a => a.UserId == userId);
    }

    // === SUPPORT ENTITIES ===

    public Comment AddComment(string content, Guid authorId)
    {
        content = content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        ValidateGuid(authorId, nameof(authorId));

        var comment = Comment.Create(content, authorId, Id);
        _comments.Add(comment);
        UpdateTimestamp();
        return comment;
    }

    public Attachment AddAttachment(string fileName, string fileUrl, string? fileType, long fileSize, Guid uploaderId)
    {
        fileName = fileName?.Trim() ?? string.Empty;
        fileUrl = fileUrl?.Trim() ?? string.Empty;
        fileType = string.IsNullOrWhiteSpace(fileType?.Trim()) ? null : fileType.Trim();

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("File URL cannot be empty.", nameof(fileUrl));
        ValidateGuid(uploaderId, nameof(uploaderId));

        var attachment = Attachment.Create(fileName, fileUrl, fileType, uploaderId, Id);
        _attachments.Add(attachment);
        UpdateTimestamp();
        return attachment;
    }

    public TimeLog LogTime(TimeSpan duration, Guid userId, string? description = null)
    {
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Duration must be positive.", nameof(duration));
        ValidateGuid(userId, nameof(userId));

        var timeLog = TimeLog.Create(duration, userId, Id);
        _timeLogs.Add(timeLog);
        UpdateTimestamp();
        return timeLog;
    }

    public Checklist CreateChecklist(string title)
    {
        title = title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));

        var checklist = Checklist.Create(title, Id);
        _checklists.Add(checklist);
        UpdateTimestamp();
        return checklist;
    }

    // === TAGS ===

    public void AddTag(Guid tagId)
    {
        ValidateGuid(tagId, nameof(tagId));

        if (_tags.Any(t => t.TagId == tagId)) return;

        var tag = ProjectTaskTag.Create(Id, tagId);
        _tags.Add(tag);
        UpdateTimestamp();
    }

    public void RemoveTag(Guid tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.TagId == tagId);
        if (tag == null) return;

        _tags.Remove(tag);
        UpdateTimestamp();
    }
    // === MOVEMENT ===

    internal void MoveToList(Guid newListId)
    {
        if (ProjectListId == newListId)
            return;

        ProjectListId = newListId;

        UpdateTimestamp();
    }

    // === VALIDATION HELPERS ===

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Task name cannot exceed 200 characters.", nameof(name));
        if (description?.Length > 2000)
            throw new ArgumentException("Task description cannot exceed 2000 characters.", nameof(description));
    }

    private static void ValidateSchedule(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
    }

    private static void ValidateEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0) throw new ArgumentOutOfRangeException(nameof(storyPoints));
        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeEstimateSeconds));
    }

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}