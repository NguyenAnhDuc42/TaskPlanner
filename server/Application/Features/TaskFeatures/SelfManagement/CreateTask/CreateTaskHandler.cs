using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        // 2. Resolve OrderKey — query max key for the parent, then compute After
        string orderKey;
        switch (request.ParentType)
        {
            case EntityLayerType.ProjectFolder:
                var maxFolderTaskKey = await UnitOfWork.Set<ProjectTask>()
                    .Where(t => t.ProjectFolderId == request.ParentId && t.DeletedAt == null)
                    .MaxAsync(t => (string?)t.OrderKey, cancellationToken);
                orderKey = maxFolderTaskKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxFolderTaskKey);
                break;
            case EntityLayerType.ProjectSpace:
                var maxSpaceTaskKey = await UnitOfWork.Set<ProjectTask>()
                    .Where(t => t.ProjectSpaceId == request.ParentId && t.ProjectFolderId == null && t.DeletedAt == null)
                    .MaxAsync(t => (string?)t.OrderKey, cancellationToken);
                orderKey = maxSpaceTaskKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxSpaceTaskKey);
                break;
            default:
                orderKey = FractionalIndex.Start();
                break;
        }

        // 3. Resolve Status (Workspace-level ownership via Workflow)
        Guid? statusId = null;
        const string statusSql = @"
            SELECT s.id 
            FROM   statuses s
            JOIN   workflows w ON s.workflow_id = w.id
            WHERE  w.project_workspace_id = @WorkspaceId 
              AND  s.deleted_at IS NULL
            ORDER BY s.created_at ASC 
            LIMIT 1";

        statusId = await UnitOfWork.QuerySingleOrDefaultAsync<Guid?>(statusSql, new { WorkspaceId = ancestors.ProjectWorkspaceId }, cancellationToken);
        
        // If user requested a status, validate it belongs to this workspace's workflow
        if (request.StatusId.HasValue)
        {
            const string validateSql = @"
                SELECT COUNT(1) 
                FROM   statuses s
                JOIN   workflows w ON s.workflow_id = w.id
                WHERE  s.id = @Id 
                  AND  w.project_workspace_id = @WorkspaceId 
                  AND  s.deleted_at IS NULL";

            var isValid = await UnitOfWork.QuerySingleOrDefaultAsync<int>(validateSql, new { Id = request.StatusId.Value, WorkspaceId = ancestors.ProjectWorkspaceId }, cancellationToken);
            if (isValid > 0) statusId = request.StatusId.Value;
        }

        // 4. Create Task
        var slug = SlugHelper.GenerateSlug(request.Name);
        var task = ProjectTask.Create(
            projectWorkspaceId: ancestors.ProjectWorkspaceId,
            projectSpaceId: ancestors.ProjectSpaceId,
            projectFolderId: ancestors.ProjectFolderId,
            name: request.Name,
            slug: slug,
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
