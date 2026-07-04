using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api;

public class BatchFlushHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    IdempotencyService idempotencyService,
    RealtimeService realtimeService,
    ILogger<BatchFlushHandler> logger
) : ICommandHandler<BatchFlushCommand, BatchFlushResult>
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // Pre-loaded entities shared across all items in the batch.
    // Bulk-loaded upfront to avoid N×entity queries.
    private record BatchContext(
        Dictionary<Guid, ProjectTask> Tasks,
        Dictionary<Guid, ProjectFolder> Folders,
        Dictionary<Guid, ProjectSpace> Spaces,
        Dictionary<Guid, Comment> Comments,
        Dictionary<Guid, Document> Documents,
        Dictionary<Guid, DocumentBlock> DocumentBlocks,
        Dictionary<Guid, TaskAssignment> Assignments,
        Dictionary<Guid, ProjectWorkspace> Workspaces
    );

    public async Task<Result<BatchFlushResult>> Handle(BatchFlushCommand request, CancellationToken ct)
    {
        var ctx = await PreloadAsync(request.Items, ct);

        var results = new List<BatchFlushItemResult>();
        var allEvents = new List<SyncEvent>();

        foreach (var item in request.Items)
        {
            try
            {
                var events = await ProcessItemAsync(item, ctx, ct);
                allEvents.AddRange(events);
                results.Add(new BatchFlushItemResult(item.TraceId, true, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Batch item {TraceId} ({EntityType}/{Action}) failed", item.TraceId, item.EntityType, item.Action);
                results.Add(new BatchFlushItemResult(item.TraceId, false, ex.Message));
            }
        }

        if (allEvents.Count > 0)
        {
            var payloads = allEvents.Select(SyncQueryService.MapToPayload).ToArray();
            _ = realtimeService
                .NotifySyncEventBatchAsync(workspaceContext.WorkspaceId, payloads, default)
                .ContinueWith(t => logger.LogError(t.Exception, "Batch DeltaBatch broadcast failed"), TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result<BatchFlushResult>.Success(new BatchFlushResult(results));
    }

    // ── Pre-load ──────────────────────────────────────────────────────────────

    private static Guid? GetGuidProp(JsonElement? data, string prop) =>
        data.HasValue && data.Value.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetGuid() : null;

    private async Task<BatchContext> PreloadAsync(List<BatchFlushItem> items, CancellationToken ct)
    {
        // Entity IDs needed for Update/Delete operations
        var taskUDIds = items.Where(i => i.EntityType == SyncEntityType.Task && i.Action != SyncAction.C).Select(i => i.EntityId).ToHashSet();
        var folderUDIds = items.Where(i => i.EntityType == SyncEntityType.Folder && i.Action != SyncAction.C).Select(i => i.EntityId).ToHashSet();
        var spaceUDIds = items.Where(i => i.EntityType == SyncEntityType.Space && i.Action != SyncAction.C).Select(i => i.EntityId).ToHashSet();
        var commentUDIds = items.Where(i => i.EntityType == SyncEntityType.Comment && i.Action != SyncAction.C).Select(i => i.EntityId).ToHashSet();
        var documentUDIds = items.Where(i => i.EntityType == SyncEntityType.Document && i.Action != SyncAction.C).Select(i => i.EntityId).ToHashSet();
        var blockUDIds = items.Where(i => i.EntityType == SyncEntityType.DocumentBlock && i.Action != SyncAction.C).Select(i => i.EntityId).ToHashSet();
        var assignmentDIds = items.Where(i => i.EntityType == SyncEntityType.Assignee && i.Action == SyncAction.D).Select(i => i.EntityId).ToHashSet();
        var workspaceUIds = items.Where(i => i.EntityType == SyncEntityType.Workspace && i.Action == SyncAction.U).Select(i => i.EntityId).ToHashSet();

        // Folders referenced by Task Creates (needed to resolve effectiveSpaceId)
        var taskCreateFolderIds = items
            .Where(i => i.EntityType == SyncEntityType.Task && i.Action == SyncAction.C)
            .Select(i => GetGuidProp(i.Data, "projectFolderId"))
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();

        // Tasks referenced by Comment/Assignee Creates (needed for workspace/space context)
        var commentCreateTaskIds = items
            .Where(i => i.EntityType == SyncEntityType.Comment && i.Action == SyncAction.C)
            .Select(i => GetGuidProp(i.Data, "projectTaskId"))
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var assigneeCreateTaskIds = items
            .Where(i => i.EntityType == SyncEntityType.Assignee && i.Action == SyncAction.C)
            .Select(i => GetGuidProp(i.Data, "taskId"))
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();

        var allTaskIds = taskUDIds.Union(commentCreateTaskIds).Union(assigneeCreateTaskIds).ToHashSet();
        var allFolderIds = folderUDIds.Union(taskCreateFolderIds).ToHashSet();

        // Documents referenced by DocumentBlock Creates
        var blockCreateDocumentIds = items
            .Where(i => i.EntityType == SyncEntityType.DocumentBlock && i.Action == SyncAction.C)
            .Select(i => GetGuidProp(i.Data, "documentId"))
            .Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
        var allDocumentIds = documentUDIds.Union(blockCreateDocumentIds).ToHashSet();

        // Bulk load entities
        var taskDict = allTaskIds.Count > 0
            ? await db.ProjectTasks.Where(t => allTaskIds.Contains(t.Id) && t.DeletedAt == null).ToDictionaryAsync(t => t.Id, ct)
            : [];
        var folderDict = allFolderIds.Count > 0
            ? await db.ProjectFolders.Where(f => allFolderIds.Contains(f.Id) && f.DeletedAt == null).ToDictionaryAsync(f => f.Id, ct)
            : [];
        var spaceDict = spaceUDIds.Count > 0
            ? await db.ProjectSpaces.Where(s => spaceUDIds.Contains(s.Id) && s.DeletedAt == null).ToDictionaryAsync(s => s.Id, ct)
            : [];
        var commentDict = commentUDIds.Count > 0
            ? await db.Comments.Where(c => commentUDIds.Contains(c.Id) && c.DeletedAt == null).ToDictionaryAsync(c => c.Id, ct)
            : [];
        var documentDict = allDocumentIds.Count > 0
            ? await db.Documents.Where(d => allDocumentIds.Contains(d.Id) && d.DeletedAt == null).ToDictionaryAsync(d => d.Id, ct)
            : [];
        var blockDict = blockUDIds.Count > 0
            ? await db.DocumentBlocks.Where(b => blockUDIds.Contains(b.Id) && b.DeletedAt == null).ToDictionaryAsync(b => b.Id, ct)
            : [];
        var assignmentDict = assignmentDIds.Count > 0
            ? await db.TaskAssignments.Where(a => assignmentDIds.Contains(a.Id) && a.DeletedAt == null).ToDictionaryAsync(a => a.Id, ct)
            : [];
        var workspaceDict = workspaceUIds.Count > 0
            ? await db.ProjectWorkspaces.Where(w => workspaceUIds.Contains(w.Id) && w.DeletedAt == null).ToDictionaryAsync(w => w.Id, ct)
            : [];

        return new BatchContext(taskDict, folderDict, spaceDict, commentDict, documentDict, blockDict, assignmentDict, workspaceDict);
    }

    private Task<List<SyncEvent>> ProcessItemAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct) =>
        (item.EntityType, item.Action) switch
        {
            (SyncEntityType.Task, SyncAction.C) => CreateTaskAsync(item, ctx, ct),
            (SyncEntityType.Task, SyncAction.U) => UpdateTaskAsync(item, ctx, ct),
            (SyncEntityType.Task, SyncAction.D) => DeleteTaskAsync(item, ctx, ct),
            (SyncEntityType.Space, SyncAction.C) => CreateSpaceAsync(item, ct),
            (SyncEntityType.Space, SyncAction.U) => UpdateSpaceAsync(item, ctx, ct),
            (SyncEntityType.Space, SyncAction.D) => DeleteSpaceAsync(item, ctx, ct),
            (SyncEntityType.Folder, SyncAction.C) => CreateFolderAsync(item, ct),
            (SyncEntityType.Folder, SyncAction.U) => UpdateFolderAsync(item, ctx, ct),
            (SyncEntityType.Folder, SyncAction.D) => DeleteFolderAsync(item, ctx, ct),
            (SyncEntityType.Comment, SyncAction.C) => CreateCommentAsync(item, ctx, ct),
            (SyncEntityType.Comment, SyncAction.U) => UpdateCommentAsync(item, ctx, ct),
            (SyncEntityType.Comment, SyncAction.D) => DeleteCommentAsync(item, ctx, ct),
            (SyncEntityType.Document, SyncAction.U) => UpdateDocumentAsync(item, ctx, ct),
            (SyncEntityType.Document, SyncAction.D) => DeleteDocumentAsync(item, ctx, ct),
            (SyncEntityType.DocumentBlock, SyncAction.C) => CreateDocumentBlockAsync(item, ctx, ct),
            (SyncEntityType.DocumentBlock, SyncAction.U) => UpdateDocumentBlockAsync(item, ctx, ct),
            (SyncEntityType.DocumentBlock, SyncAction.D) => DeleteDocumentBlockAsync(item, ctx, ct),
            (SyncEntityType.Assignee, SyncAction.C) => CreateAssigneeAsync(item, ctx, ct),
            (SyncEntityType.Assignee, SyncAction.D) => DeleteAssigneeAsync(item, ctx, ct),
            (SyncEntityType.Workspace, SyncAction.U) => UpdateWorkspaceAsync(item, ctx, ct),
            _ => throw new InvalidOperationException($"Unsupported batch item: {item.EntityType}/{item.Action}")
        };

    private T Deserialize<T>(JsonElement? raw) where T : class =>
        raw is null
            ? throw new InvalidOperationException("Data payload is required")
            : JsonSerializer.Deserialize<T>(raw.Value.GetRawText(), JsonOpts)
              ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");

    // ── Task ──────────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> CreateTaskAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        syncPermission.RequireMember();

        var cmd = Deserialize<CreateTaskCommand>(item.Data);
        cmd.TraceId = item.TraceId;

        var effectiveSpaceId = cmd.ProjectSpaceId!.Value;
        var effectiveFolderId = cmd.ProjectFolderId;
        if (cmd.ProjectFolderId.HasValue)
        {
            // Try pre-loaded dict first; fall back if folder was created earlier in this same batch
            if (!ctx.Folders.TryGetValue(cmd.ProjectFolderId.Value, out var folder))
            {
                folder = await db.ProjectFolders.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == cmd.ProjectFolderId.Value && f.DeletedAt == null, ct);
            }
            if (folder is null) throw new InvalidOperationException(FolderError.NotFound.Code);
            effectiveSpaceId = folder.ProjectSpaceId;
        }

        // A subtask always lives in the exact same space/folder as its parent task — mirrors the
        // online CreateTaskHandler's "never trust the client for ancestor scope" rule.
        if (cmd.ParentTaskId.HasValue)
        {
            if (!ctx.Tasks.TryGetValue(cmd.ParentTaskId.Value, out var parentTask))
            {
                parentTask = await db.ProjectTasks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == cmd.ParentTaskId.Value && t.DeletedAt == null, ct);
            }
            if (parentTask is null || !parentTask.ProjectSpaceId.HasValue) throw new InvalidOperationException(TaskError.NotFound.Code);
            effectiveSpaceId = parentTask.ProjectSpaceId.Value;
            effectiveFolderId = parentTask.ProjectFolderId;
        }

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var document = Document.Create(id: cmd.DefaultDocumentId, workspaceId: workspaceContext.WorkspaceId, name: cmd.Name, creatorId: creatorId);
            db.Documents.Add(document);

            var task = ProjectTask.Create(
                id: cmd.Id, projectWorkspaceId: workspaceContext.WorkspaceId, projectSpaceId: effectiveSpaceId,
                projectFolderId: effectiveFolderId, name: cmd.Name, slug: cmd.Slug,
                defaultDocumentId: document.Id, color: cmd.Color ?? "#FFFFFF", icon: cmd.Icon,
                creatorId: creatorId, statusId: cmd.StatusId, priority: cmd.Priority,
                orderKey: cmd.OrderKey, parentTaskId: cmd.ParentTaskId);
            db.ProjectTasks.Add(task);

            events.AddRange([
                new SyncEvent {
                    ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Task,
                    EntityId = task.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                    Payload = JsonSerializer.Serialize(new {
                        id = task.Id, workspaceId = task.ProjectWorkspaceId, spaceId = effectiveSpaceId,
                        folderId = task.ProjectFolderId, name = task.Name, slug = task.Slug,
                        defaultDocumentId = task.DefaultDocumentId, color = task.Color, icon = task.Icon,
                        statusId = task.StatusId, priority = task.Priority, orderKey = task.OrderKey, parentTaskId = task.ParentTaskId
                    }, JsonOpts)
                }
            ]);
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> UpdateTaskAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Tasks.TryGetValue(item.EntityId, out var task))
            throw new InvalidOperationException(TaskError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(task.CreatorId ?? Guid.Empty);

        var cmd = Deserialize<UpdateTaskCommand>(item.Data);
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var slug = cmd.Name != null ? SlugHelper.GenerateSlug(cmd.Name) : null;
            task.Update(cmd.Name, slug, cmd.Color, cmd.Icon, cmd.StatusId, cmd.Priority,
                cmd.StartDate, cmd.ClearStartDate, cmd.DueDate, cmd.ClearDueDate,
                cmd.StoryPoints, cmd.TimeEstimateSeconds, cmd.OrderKey, cmd.ParentTaskId,
                cmd.SpaceId, cmd.FolderId);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = task.ProjectWorkspaceId, EntityType = SyncEntityType.Task,
                EntityId = task.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new {
                    id = task.Id, workspaceId = task.ProjectWorkspaceId, spaceId = task.ProjectSpaceId,
                    folderId = task.ProjectFolderId, name = task.Name, slug = task.Slug,
                    defaultDocumentId = task.DefaultDocumentId, color = task.Color, icon = task.Icon,
                    statusId = task.StatusId, priority = task.Priority, startDate = task.StartDate,
                    dueDate = task.DueDate, storyPoints = task.StoryPoints, timeEstimateSeconds = task.TimeEstimateSeconds,
                    orderKey = task.OrderKey, parentTaskId = task.ParentTaskId, isArchived = task.IsArchived
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteTaskAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Tasks.TryGetValue(item.EntityId, out var task))
            throw new InvalidOperationException(TaskError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(task.CreatorId ?? Guid.Empty);

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            task.SoftDelete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = task.ProjectWorkspaceId, EntityType = SyncEntityType.Task,
                EntityId = task.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = task.Id }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── Space ─────────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> CreateSpaceAsync(BatchFlushItem item, CancellationToken ct)
    {
        syncPermission.RequireMember();

        var cmd = Deserialize<CreateSpaceCommand>(item.Data);
        var creatorId = workspaceContext.CurrentMember!.Id;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var maxKey = await db.ProjectSpaces.AsNoTracking()
                .Where(s => s.ProjectWorkspaceId == workspaceContext.WorkspaceId && s.DeletedAt == null)
                .Select(s => (string?)s.OrderKey).MaxAsync(ct);
            var orderKey = FractionalIndex.SafeAfter(maxKey);
            var slug = SlugHelper.GenerateSlug(cmd.Name);

            var document = Document.Create(id: cmd.DefaultDocumentId, workspaceId: workspaceContext.WorkspaceId, name: cmd.Name, creatorId: creatorId);
            db.Documents.Add(document);

            var space = ProjectSpace.Create(id: cmd.Id, projectWorkspaceId: workspaceContext.WorkspaceId,
                name: cmd.Name, slug: slug, defaultDocumentId: document.Id, color: cmd.Color,
                icon: cmd.Icon, isPrivate: cmd.IsPrivate, creatorId: creatorId, orderKey: orderKey);
            db.ProjectSpaces.Add(space);
            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Space,
                EntityId = space.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                Payload = JsonSerializer.Serialize(new {
                    id = space.Id, workspaceId = workspaceContext.WorkspaceId, name = space.Name, slug = space.Slug,
                    color = space.Color, icon = space.Icon, isPrivate = space.IsPrivate,
                    orderKey = space.OrderKey, defaultDocumentId = space.DefaultDocumentId
                }, JsonOpts)
            });

            var statuses = Status.CreateSpaceStarterSet(workspaceContext.WorkspaceId, space.Id, creatorId);
            db.Statuses.AddRange(statuses);
            foreach (var status in statuses)
            {
                events.Add(new SyncEvent {
                    ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Status,
                    EntityId = status.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                    Payload = JsonSerializer.Serialize(new {
                        id = status.Id, spaceId = status.ProjectSpaceId, name = status.Name,
                        color = status.Color, category = status.Category.ToString(), orderKey = status.OrderKey
                    }, JsonOpts)
                });
            }

            var creatorAccess = EntityAccess.Create(projectWorkspaceId: workspaceContext.WorkspaceId,
                workspaceMemberId: creatorId, projectSpaceId: space.Id, projectFolderId: null,
                projectTaskId: null, accessLevel: AccessLevel.Manager, creatorId: creatorId);
            db.EntityAccesses.Add(creatorAccess);
            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.EntityAccess,
                EntityId = creatorAccess.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                Payload = JsonSerializer.Serialize(new {
                    id = creatorAccess.Id, workspaceMemberId = creatorAccess.WorkspaceMemberId,
                    spaceId = creatorAccess.ProjectSpaceId, accessLevel = creatorAccess.AccessLevel.ToString(), haveAccess = true
                }, JsonOpts)
            });

            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> UpdateSpaceAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Spaces.TryGetValue(item.EntityId, out var space))
            throw new InvalidOperationException(SpaceError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(space.CreatorId ?? Guid.Empty);

        var cmd = Deserialize<UpdateSpaceCommand>(item.Data);
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var slug = cmd.Name != null ? SlugHelper.GenerateSlug(cmd.Name) : null;
            space.Update(cmd.Name, slug, cmd.Color, cmd.Icon, cmd.IsPrivate, cmd.OrderKey);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = space.ProjectWorkspaceId, EntityType = SyncEntityType.Space,
                EntityId = space.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new {
                    id = space.Id, workspaceId = space.ProjectWorkspaceId, name = space.Name, slug = space.Slug,
                    color = space.Color, icon = space.Icon, isPrivate = space.IsPrivate,
                    orderKey = space.OrderKey, defaultDocumentId = space.DefaultDocumentId, isArchived = space.IsArchived
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteSpaceAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Spaces.TryGetValue(item.EntityId, out var space))
            throw new InvalidOperationException(SpaceError.NotFound.Code);

        syncPermission.RequireAdmin();

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var now = DateTimeOffset.UtcNow;
            await db.ProjectTasks.Where(t => t.ProjectSpaceId == space.Id && t.DeletedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.DeletedAt, now).SetProperty(t => t.UpdatedAt, now), ct);
            await db.ProjectFolders.Where(f => f.ProjectSpaceId == space.Id && f.DeletedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(f => f.DeletedAt, now).SetProperty(f => f.UpdatedAt, now), ct);
            await db.Statuses.Where(st => st.ProjectSpaceId == space.Id && st.DeletedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(st => st.DeletedAt, now).SetProperty(st => st.UpdatedAt, now), ct);

            space.Delete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = space.ProjectWorkspaceId, EntityType = SyncEntityType.Space,
                EntityId = space.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = space.Id }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── Folder ────────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> CreateFolderAsync(BatchFlushItem item, CancellationToken ct)
    {
        syncPermission.RequireMember();

        var cmd = Deserialize<CreateFolderCommand>(item.Data);
        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var maxKey = await db.ProjectFolders.AsNoTracking()
                .Where(f => f.ProjectSpaceId == cmd.SpaceId && f.DeletedAt == null)
                .Select(f => (string?)f.OrderKey).OrderByDescending(k => k).FirstOrDefaultAsync(ct);
            var orderKey = FractionalIndex.SafeAfter(maxKey);
            var slug = SlugHelper.GenerateSlug(cmd.Name);

            var folder = ProjectFolder.Create(id: cmd.Id, projectWorkspaceId: workspaceContext.WorkspaceId,
                projectSpaceId: cmd.SpaceId, name: cmd.Name, slug: slug, orderKey: orderKey,
                creatorId: creatorId, color: cmd.Color, icon: cmd.Icon, startDate: cmd.StartDate, dueDate: cmd.DueDate);
            db.ProjectFolders.Add(folder);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Folder,
                EntityId = folder.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                Payload = JsonSerializer.Serialize(new {
                    id = folder.Id, workspaceId = workspaceContext.WorkspaceId, spaceId = folder.ProjectSpaceId,
                    name = folder.Name, slug = folder.Slug, color = folder.Color, icon = folder.Icon,
                    orderKey = folder.OrderKey, startDate = folder.StartDate, dueDate = folder.DueDate
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> UpdateFolderAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Folders.TryGetValue(item.EntityId, out var folder))
            throw new InvalidOperationException(FolderError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(folder.CreatorId ?? Guid.Empty);

        var cmd = Deserialize<UpdateFolderCommand>(item.Data);
        var oldSpaceId = folder.ProjectSpaceId;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var slug = cmd.Name != null ? SlugHelper.GenerateSlug(cmd.Name) : null;
            folder.Update(name: cmd.Name, slug: slug, color: cmd.Color, icon: cmd.Icon,
                startDate: cmd.StartDate, dueDate: cmd.DueDate, orderKey: cmd.OrderKey,
                clearStartDate: cmd.ClearStartDate, clearDueDate: cmd.ClearDueDate,
                spaceId: cmd.SpaceId);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = folder.ProjectWorkspaceId, EntityType = SyncEntityType.Folder,
                EntityId = folder.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new {
                    id = folder.Id, workspaceId = folder.ProjectWorkspaceId, spaceId = folder.ProjectSpaceId,
                    name = folder.Name, slug = folder.Slug, color = folder.Color, icon = folder.Icon,
                    orderKey = folder.OrderKey, startDate = folder.StartDate, dueDate = folder.DueDate
                }, JsonOpts)
            });

            // Cascade: same reasoning as UpdateFolderHandler — a folder moving to a different
            // space takes its tasks with it, or they're silently invisible / wrongly cascade-
            // deleted when the old space goes away.
            if (oldSpaceId != folder.ProjectSpaceId)
            {
                var childTasks = await db.ProjectTasks
                    .Where(t => t.ProjectFolderId == folder.Id && t.DeletedAt == null)
                    .ToListAsync(ct);

                foreach (var task in childTasks)
                {
                    task.Update(spaceId: folder.ProjectSpaceId);

                    events.Add(new SyncEvent {
                        ProjectWorkspaceId = task.ProjectWorkspaceId, EntityType = SyncEntityType.Task,
                        EntityId = task.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                        Payload = JsonSerializer.Serialize(new {
                            id = task.Id, workspaceId = task.ProjectWorkspaceId, spaceId = task.ProjectSpaceId,
                            folderId = task.ProjectFolderId, name = task.Name, slug = task.Slug,
                            defaultDocumentId = task.DefaultDocumentId, color = task.Color, icon = task.Icon,
                            statusId = task.StatusId, priority = task.Priority, startDate = task.StartDate,
                            dueDate = task.DueDate, storyPoints = task.StoryPoints, timeEstimateSeconds = task.TimeEstimateSeconds,
                            orderKey = task.OrderKey, parentTaskId = task.ParentTaskId, isArchived = task.IsArchived
                        }, JsonOpts)
                    });
                }
            }

            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteFolderAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Folders.TryGetValue(item.EntityId, out var folder))
            throw new InvalidOperationException(FolderError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(folder.CreatorId ?? Guid.Empty);

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var orphanedTasks = await db.ProjectTasks.AsNoTracking()
                .Where(t => t.ProjectFolderId == folder.Id && t.DeletedAt == null).ToListAsync(ct);

            if (orphanedTasks.Count > 0)
                await db.ProjectTasks.Where(t => t.ProjectFolderId == folder.Id && t.DeletedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.ProjectFolderId, (Guid?)null), ct);

            foreach (var task in orphanedTasks)
            {
                events.Add(new SyncEvent {
                    ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Task,
                    EntityId = task.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                    Payload = JsonSerializer.Serialize(new {
                        id = task.Id, workspaceId = task.ProjectWorkspaceId, spaceId = task.ProjectSpaceId,
                        folderId = (Guid?)null, name = task.Name, slug = task.Slug,
                        defaultDocumentId = task.DefaultDocumentId, color = task.Color, icon = task.Icon,
                        statusId = task.StatusId, priority = task.Priority, orderKey = task.OrderKey,
                        parentTaskId = task.ParentTaskId, isArchived = task.IsArchived
                    }, JsonOpts)
                });
            }

            folder.Delete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Folder,
                EntityId = folder.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = folder.Id }, JsonOpts)
            });

            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── Comment ───────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> CreateCommentAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var cmd = Deserialize<CreateCommentCommand>(item.Data);
        if (!ctx.Tasks.ContainsKey(cmd.ProjectTaskId))
            throw new InvalidOperationException(TaskError.NotFound.Code);

        syncPermission.RequireMember();

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var comment = Comment.Create(cmd.Id, cmd.Content, creatorId, cmd.ProjectTaskId, cmd.ParentCommentId);
            db.Comments.Add(comment);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Comment,
                EntityId = comment.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                Payload = JsonSerializer.Serialize(new {
                    id = comment.Id, taskId = comment.ProjectTaskId, content = comment.Content,
                    isEdited = comment.IsEdited, parentCommentId = comment.ParentCommentId,
                    creatorId = comment.CreatorId, createdAt = comment.CreatedAt
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> UpdateCommentAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Comments.TryGetValue(item.EntityId, out var comment))
            throw new InvalidOperationException(CommentError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(comment.CreatorId ?? Guid.Empty);

        var cmd = Deserialize<UpdateCommentCommand>(item.Data);
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            comment.UpdateContent(cmd.Content);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Comment,
                EntityId = comment.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new {
                    id = comment.Id, taskId = comment.ProjectTaskId, content = comment.Content,
                    isEdited = comment.IsEdited, parentCommentId = comment.ParentCommentId,
                    creatorId = comment.CreatorId, createdAt = comment.CreatedAt
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteCommentAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Comments.TryGetValue(item.EntityId, out var comment))
            throw new InvalidOperationException(CommentError.NotFound.Code);

        syncPermission.RequireCreatorOrAdmin(comment.CreatorId ?? Guid.Empty);

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            comment.SoftDelete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Comment,
                EntityId = comment.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = comment.Id }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── Document ──────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> UpdateDocumentAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Documents.TryGetValue(item.EntityId, out var document))
            throw new InvalidOperationException(DocumentError.NotFound.Code);

        syncPermission.RequireMember();

        var cmd = Deserialize<UpdateDocumentCommand>(item.Data);
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            document.UpdateName(cmd.Name);
            events.Add(new SyncEvent {
                ProjectWorkspaceId = document.ProjectWorkspaceId, EntityType = SyncEntityType.Document,
                EntityId = document.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = document.Id, workspaceId = document.ProjectWorkspaceId, name = document.Name }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteDocumentAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Documents.TryGetValue(item.EntityId, out var document))
            throw new InvalidOperationException(DocumentError.NotFound.Code);

        syncPermission.RequireMember();

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            document.SoftDelete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = document.ProjectWorkspaceId, EntityType = SyncEntityType.Document,
                EntityId = document.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = document.Id }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── DocumentBlock ─────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> CreateDocumentBlockAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var cmd = Deserialize<CreateDocumentBlockCommand>(item.Data);
        if (!ctx.Documents.ContainsKey(cmd.DocumentId))
            throw new InvalidOperationException(DocumentError.NotFound.Code);

        syncPermission.RequireMember();

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var block = DocumentBlock.CreateWithId(cmd.Id, workspaceContext.WorkspaceId, cmd.DocumentId, cmd.Type, cmd.Content, cmd.OrderKey, creatorId);
            db.DocumentBlocks.Add(block);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.DocumentBlock,
                EntityId = block.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                Payload = JsonSerializer.Serialize(new {
                    id = block.Id, documentId = block.DocumentId, workspaceId = block.ProjectWorkspaceId,
                    type = block.Type, content = block.Content, orderKey = block.OrderKey
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> UpdateDocumentBlockAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.DocumentBlocks.TryGetValue(item.EntityId, out var block))
            throw new InvalidOperationException(DocumentBlockError.NotFound.Code);

        syncPermission.RequireMember();

        var cmd = Deserialize<UpdateDocumentBlockCommand>(item.Data);
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            if (cmd.Content is not null) block.UpdateContent(cmd.Content);
            if (cmd.OrderKey is not null) block.UpdateOrderKey(cmd.OrderKey);
            if (cmd.Type.HasValue) block.UpdateType(cmd.Type.Value);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = block.ProjectWorkspaceId, EntityType = SyncEntityType.DocumentBlock,
                EntityId = block.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new {
                    id = block.Id, documentId = block.DocumentId, workspaceId = block.ProjectWorkspaceId,
                    type = block.Type, content = block.Content, orderKey = block.OrderKey
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteDocumentBlockAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.DocumentBlocks.TryGetValue(item.EntityId, out var block))
            throw new InvalidOperationException(DocumentBlockError.NotFound.Code);

        syncPermission.RequireMember();

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            block.SoftDelete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = block.ProjectWorkspaceId, EntityType = SyncEntityType.DocumentBlock,
                EntityId = block.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = block.Id }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── Assignee ──────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> CreateAssigneeAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var cmd = Deserialize<CreateAssigneeCommand>(item.Data);
        if (!ctx.Tasks.ContainsKey(cmd.TaskId))
            throw new InvalidOperationException(TaskError.NotFound.Code);

        syncPermission.RequireMember();

        var creatorId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var alreadyAssigned = await db.TaskAssignments.AnyAsync(
                a => a.ProjectTaskId == cmd.TaskId && a.WorkspaceMemberId == cmd.MemberId && a.DeletedAt == null, ct);
            if (alreadyAssigned) return Result<int>.Success(0);

            var assignment = TaskAssignment.Create(cmd.Id, cmd.TaskId, cmd.MemberId, creatorId);
            db.TaskAssignments.Add(assignment);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Assignee,
                EntityId = assignment.Id, Action = SyncAction.C, ClientTraceId = item.TraceId, AuthorUserId = creatorId,
                Payload = JsonSerializer.Serialize(new {
                    id = assignment.Id, taskId = assignment.ProjectTaskId, workspaceMemberId = assignment.WorkspaceMemberId
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    private async Task<List<SyncEvent>> DeleteAssigneeAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        if (!ctx.Assignments.TryGetValue(item.EntityId, out var assignment))
            throw new InvalidOperationException(AssigneeError.NotFound.Code);

        syncPermission.RequireMember();

        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            assignment.SoftDelete();
            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspaceContext.WorkspaceId, EntityType = SyncEntityType.Assignee,
                EntityId = assignment.Id, Action = SyncAction.D, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new { id = assignment.Id }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

    // ── Workspace ─────────────────────────────────────────────────────────────

    private async Task<List<SyncEvent>> UpdateWorkspaceAsync(BatchFlushItem item, BatchContext ctx, CancellationToken ct)
    {
        if (!ctx.Workspaces.TryGetValue(item.EntityId, out var workspace))
            throw new InvalidOperationException(WorkspaceError.NotFound.Code);

        // Owner-only — NOT RequireCreatorOrAdmin: ProjectWorkspace.CreatorId stores the creating
        // User's Id (audit field predates workspace membership existing), not a WorkspaceMember.Id
        // like every other entity's CreatorId. Comparing it against member.Id would always fail.
        if (workspace.CreatorId != workspaceContext.CurrentMember?.UserId)
            throw new UnauthorizedAccessException(MemberError.DontHavePermission.Code);

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var cmd = Deserialize<UpdateWorkspaceCommand>(item.Data);
        List<SyncEvent> events = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            if (await idempotencyService.HasProcessedAsync(item.TraceId, ct)) return Result<int>.Success(0);

            var slug = cmd.Name != null ? SlugHelper.GenerateSlug(cmd.Name) : null;
            workspace.Update(cmd.Name, slug, cmd.Description, cmd.Color, cmd.Icon, cmd.StrictJoin);

            events.Add(new SyncEvent {
                ProjectWorkspaceId = workspace.Id, EntityType = SyncEntityType.Workspace,
                EntityId = workspace.Id, Action = SyncAction.U, ClientTraceId = item.TraceId, AuthorUserId = memberId,
                Payload = JsonSerializer.Serialize(new {
                    id = workspace.Id, name = workspace.Name, description = workspace.Description,
                    color = workspace.Color, icon = workspace.Icon, strictJoin = workspace.StrictJoin
                }, JsonOpts)
            });
            db.SyncEvents.AddRange(events);
            idempotencyService.MarkAsProcessed(item.TraceId);
            return Result<int>.Success(0);
        }, ct);

        if (!result.IsSuccess) throw new InvalidOperationException(result.Error?.Description ?? "Transaction failed");
        return events;
    }

}
