using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using MediatR;
using server.Application.Interfaces;
using Domain.Enums.RelationShip;
using Application.Features.ViewFeatures.GetViewData;

namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

public class CreateTaskHandler : BaseFeatureHandler, IRequestHandler<CreateTaskCommand, TaskDto>
{
    public CreateTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve Ancestors (Dapper-based Helper)
        var ancestors = await HierarchyHelper.GetAncestorChain(UnitOfWork, request.ParentId, request.ParentType, cancellationToken);    
        long orderKey;
        // 2. Resolve OrderKey (Fetch from parent)
        switch (request.ParentType)
        {
            case EntityLayerType.ProjectFolder:
                var folder = await UnitOfWork.Set<ProjectFolder>().FindAsync(request.ParentId, cancellationToken);
                if (folder == null) throw new KeyNotFoundException($"Folder {request.ParentId} not found");
                orderKey = folder.GetNextItemOrderAndIncrement();
                break;
            case EntityLayerType.ProjectSpace:
                var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.ParentId, cancellationToken);
                if (space == null) throw new KeyNotFoundException($"Space {request.ParentId} not found");
                orderKey = space.GetNextItemOrderAndIncrement();
                break;
            default:
                orderKey = 10_000_000L;
                break;
        }

        // 3. Resolve Status (Space-only ownership via Workflow)
        Guid? statusId = null;
        if (ancestors.ProjectSpaceId.HasValue)
        {
            const string statusSql = @"
                SELECT s.id 
                FROM   statuses s
                JOIN   workflows w ON s.workflow_id = w.id
                WHERE  w.project_space_id = @SpaceId 
                  AND  s.deleted_at IS NULL
                ORDER BY s.created_at ASC 
                LIMIT 1";

            statusId = await UnitOfWork.QuerySingleOrDefaultAsync<Guid?>(statusSql, new { SpaceId = ancestors.ProjectSpaceId.Value }, cancellationToken);
            
            // If user requested a status, validate it belongs to this space's workflow
            if (request.StatusId.HasValue)
            {
                const string validateSql = @"
                    SELECT COUNT(1) 
                    FROM   statuses s
                    JOIN   workflows w ON s.workflow_id = w.id
                    WHERE  s.id = @Id 
                      AND  w.project_space_id = @SpaceId 
                      AND  s.deleted_at IS NULL";

                var isValid = await UnitOfWork.QuerySingleOrDefaultAsync<int>(validateSql, new { Id = request.StatusId.Value, SpaceId = ancestors.ProjectSpaceId.Value }, cancellationToken);
                if (isValid > 0) statusId = request.StatusId.Value;
            }
        }

        // 4. Create Task
        var task = ProjectTask.Create(
            projectWorkspaceId: ancestors.ProjectWorkspaceId,
            projectSpaceId: ancestors.ProjectSpaceId,
            projectFolderId: ancestors.ProjectFolderId,
            name: request.Name,
            description: request.Description,
            customization: null,
            creatorId: CurrentUserId,
            statusId: statusId,
            priority: request.Priority,
            orderKey: orderKey,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            storyPoints: request.StoryPoints,
            timeEstimate: request.TimeEstimate
        );

        await UnitOfWork.Set<ProjectTask>().AddAsync(task, cancellationToken);

        // 5. Assignments
        var assignees = new List<AssigneeDto>();
        if (request.AssigneeIds?.Any() == true)
        {
            var validUserIds = await ValidateWorkspaceMembers(request.AssigneeIds, cancellationToken);
            var memberIds = await GetWorkspaceMemberIds(validUserIds, cancellationToken);

            var accessibleMemberIds = await GetAccessibleMemberIds(request.ParentId, request.ParentType, memberIds);

            if (accessibleMemberIds.Count != memberIds.Count)
            {
                throw new System.ComponentModel.DataAnnotations.ValidationException("One or more assignees do not have permission to access this container.");
            }

            var assignments = accessibleMemberIds.Select(memberId => TaskAssignment.Create(task.Id, memberId, CurrentUserId));
            task.AddAsignees(assignments.ToList());

            var details = await UnitOfWork.QueryAsync<AssigneeDto>(@"
                SELECT u.id AS Id, u.name AS Name, NULL AS AvatarUrl
                FROM users u
                JOIN workspace_members wm ON wm.user_id = u.id
                WHERE wm.id = ANY(@MemberIds)", new { MemberIds = accessibleMemberIds.ToArray() });
            assignees = details.ToList();
        }

        return new TaskDto
        {
            Id = task.Id,
            ProjectWorkspaceId = task.ProjectWorkspaceId,
            ProjectSpaceId = task.ProjectSpaceId,
            ProjectFolderId = task.ProjectFolderId,
            Name = task.Name,
            Description = task.Description,
            StatusId = task.StatusId,
            Priority = task.Priority,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            StoryPoints = task.StoryPoints,
            TimeEstimate = task.TimeEstimate,
            OrderKey = task.OrderKey,
            CreatedAt = task.CreatedAt,
            Assignees = assignees
        };
    }
}
