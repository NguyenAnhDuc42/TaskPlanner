    namespace Domain;

    public class ProjectTask : TenantEntity
    {
        public Guid? ProjectSpaceId { get; private set; }
        public Guid? ProjectFolderId { get; private set; }
        public Guid? ParentTaskId { get; private set; }
        public string Name { get; private set; } = null!;
        public string Slug { get; private set; } = null!;
        public Guid DefaultDocumentId { get; private set; }
        public string Color { get; private set; } = "#FFFFFF";
        public string? Icon { get; private set; }
        public Guid? StatusId { get; private set; }
        public bool IsArchived { get; private set; }
        public Priority Priority { get; private set; } = Priority.Low;
        public DateTimeOffset? StartDate { get; private set; }
        public DateTimeOffset? DueDate { get; private set; }
        public int? StoryPoints { get; private set; }
        public long? TimeEstimateSeconds { get; private set; }
        public string OrderKey { get; private set; } = null!;

        private readonly List<TaskAssignment> _assignees = new();
        public virtual IReadOnlyCollection<TaskAssignment> Assignees => _assignees.AsReadOnly();    

        // EF Core
        private ProjectTask() { }

        private ProjectTask(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, Guid defaultDocumentId, string color, string? icon, Guid creatorId, Guid? statusId, Priority priority, DateTimeOffset? startDate, DateTimeOffset? dueDate, int? storyPoints, long? timeEstimateSeconds, string orderKey, Guid? parentTaskId = null)
            : base(id, projectWorkspaceId)
        {
            ProjectSpaceId = projectSpaceId;
            ProjectFolderId = projectFolderId;
            Name = name;
            Slug = slug;
            DefaultDocumentId = defaultDocumentId;
            Color = color;
            Icon = icon;
            StatusId = statusId;
            Priority = priority;
            StartDate = startDate;
            DueDate = dueDate;
            StoryPoints = storyPoints;
            TimeEstimateSeconds = timeEstimateSeconds;
            OrderKey = orderKey;
            IsArchived = false;
            ParentTaskId = parentTaskId;

            // Audit is initialized in base constructor
            InitializeAudit(creatorId);
        }

        public static ProjectTask Create(Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, Guid defaultDocumentId, string? color, string? icon, Guid creatorId, Guid? statusId = null, Priority priority = Priority.Low, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, int? storyPoints = null, long? timeEstimateSeconds = null, string? orderKey = null, Guid? parentTaskId = null)
        {
            return new ProjectTask(
                projectWorkspaceId: projectWorkspaceId,
                projectSpaceId: projectSpaceId,
                projectFolderId: projectFolderId,
                name: name,
                slug: slug,
                defaultDocumentId: defaultDocumentId,
                color: color ?? "#FFFFFF",
                icon: icon,
                creatorId: creatorId,
                statusId: statusId,
                priority: priority,
                startDate: startDate,
                dueDate: dueDate,
                storyPoints: storyPoints,
                timeEstimateSeconds: timeEstimateSeconds,
                orderKey: orderKey ?? FractionalIndex.Start(),
                parentTaskId: parentTaskId,
                id: Guid.NewGuid());
        }
        /// Prefer way to create an entity with a pre-defined ID.
        public static ProjectTask Create(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, Guid defaultDocumentId, string? color, string? icon, Guid creatorId, Guid? statusId = null, Priority priority = Priority.Low, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, int? storyPoints = null, long? timeEstimateSeconds = null, string? orderKey = null, Guid? parentTaskId = null)
        {
            if (id == Guid.Empty) throw new BusinessRuleException("Id cannot be empty.");

            return new ProjectTask(
                projectWorkspaceId: projectWorkspaceId,
                projectSpaceId: projectSpaceId,
                projectFolderId: projectFolderId,
                name: name,
                slug: slug,
                defaultDocumentId: defaultDocumentId,
                color: color ?? "#FFFFFF",
                icon: icon,
                creatorId: creatorId,
                statusId: statusId,
                priority: priority,
                startDate: startDate,
                dueDate: dueDate,
                storyPoints: storyPoints,
                timeEstimateSeconds: timeEstimateSeconds,
                orderKey: orderKey ?? FractionalIndex.Start(),
                parentTaskId: parentTaskId,
                id: id);
        }

        public static List<ProjectTask> CreateDefaults(Guid projectWorkspaceId, Guid spaceId, Guid folderId, Guid statusId, Guid creatorId, Guid exploreDocId, Guid standaloneDocId)
        {
            var start = FractionalIndex.Start();
            return new List<ProjectTask>
            {
                Create(
                    projectWorkspaceId: projectWorkspaceId,
                    projectSpaceId: spaceId,
                    projectFolderId: folderId,
                    name: "Explore the hierarchy",
                    slug: "explore-hierarchy",
                    defaultDocumentId: exploreDocId,
                    color: null,
                    icon: null,
                    creatorId: creatorId,
                    statusId: statusId,
                    orderKey: start
                ),
                Create(
                    projectWorkspaceId: projectWorkspaceId,
                    projectSpaceId: spaceId,
                    projectFolderId: null,
                    name: "Standalone Task",
                    slug: "standalone-task",
                    defaultDocumentId: standaloneDocId,
                    color: null,
                    icon: null,
                    creatorId: creatorId,
                    statusId: statusId,
                    orderKey: FractionalIndex.After(start)
                )
            };
        }

        public void Update(
            string? name = null,
            string? slug = null,
            string? color = null,
            string? icon = null,
            Guid? statusId = null,
            Priority? priority = null,
            DateTimeOffset? startDate = null,
            bool clearStartDate = false,
            DateTimeOffset? dueDate = null,
            bool clearDueDate = false,
            int? storyPoints = null,
            long? timeEstimateSeconds = null,
            string? orderKey = null,
            Guid? parentTaskId = null,
            Guid? spaceId = null,
            Guid? folderId = null)
        {
            EnsureNotArchived();
            bool updated = false;

            if (name != null && Name != name) { Name = name; updated = true; }
            if (slug != null && Slug != slug) { Slug = slug; updated = true; }
            if (color != null && Color != color) { Color = color; updated = true; }
            if (icon != null && Icon != icon) { Icon = icon; updated = true; }
            if (statusId.HasValue)
            {
                var newStatus = statusId.Value == Guid.Empty ? null : (Guid?)statusId.Value;
                if (StatusId != newStatus) { StatusId = newStatus; updated = true; }
            }
            // spaceId has no "clear" sentinel — a task always belongs to some space.
            if (spaceId.HasValue && spaceId.Value != Guid.Empty && ProjectSpaceId != spaceId) { ProjectSpaceId = spaceId; updated = true; }
            // folderId: Guid.Empty clears it (task moves directly under the space, no folder) —
            // same sentinel convention as statusId above, since Guid? params can't distinguish
            // "not touched" (null) from "explicitly cleared" any other way.
            if (folderId.HasValue)
            {
                var newFolder = folderId.Value == Guid.Empty ? null : (Guid?)folderId.Value;
                if (ProjectFolderId != newFolder) { ProjectFolderId = newFolder; updated = true; }
            }
            if (priority != null && Priority != priority) { Priority = priority.Value; updated = true; }
            if (clearStartDate && StartDate != null) { StartDate = null; updated = true; }
            else if (startDate != null && StartDate != startDate) { StartDate = startDate; updated = true; }
            
            if (clearDueDate && DueDate != null) { DueDate = null; updated = true; }
            else if (dueDate != null && DueDate != dueDate) { DueDate = dueDate; updated = true; }
            if (storyPoints != null && StoryPoints != storyPoints) { StoryPoints = storyPoints; updated = true; }
            if (timeEstimateSeconds != null && TimeEstimateSeconds != timeEstimateSeconds) { TimeEstimateSeconds = timeEstimateSeconds; updated = true; }
            if (orderKey != null && OrderKey != orderKey) { OrderKey = orderKey; updated = true; }
            if (parentTaskId != null && ParentTaskId != parentTaskId) { ParentTaskId = parentTaskId; updated = true; }

            if (updated) UpdateTimestamp();
        }

        public void AddAsignees(List<TaskAssignment> newAssignments)
        {
            EnsureNotArchived();
            foreach (var assignment in newAssignments)
            {
                if (!_assignees.Any(a => a.WorkspaceMemberId == assignment.WorkspaceMemberId))
                {
                    _assignees.Add(assignment);
                }
            }
            if (newAssignments.Count > 0) UpdateTimestamp();
        }

        public void RemoveAsignees(List<Guid> memberIdsToRemove)
        {
            EnsureNotArchived();
            var removed = _assignees.RemoveAll(a => memberIdsToRemove.Contains(a.WorkspaceMemberId));
            if (removed > 0) UpdateTimestamp();
        }

        public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
        public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }

        private void EnsureNotArchived()
        {
            if (IsArchived) throw new BusinessRuleException("Cannot modify an archived task.");
        }
    }


