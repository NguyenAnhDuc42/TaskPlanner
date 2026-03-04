using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using MediatR;
using server.Application.Interfaces;
using Application.Contract.Common;
using Domain.Enums.RelationShip;

namespace Application.Features.TaskFeatures.SelfManagement.CreateTask;

public class CreateTaskHandler : BaseFeatureHandler, IRequestHandler<CreateTaskCommand, TaskDto>
{
    public CreateTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(request.ListId);

        var statusId = await ResolveTaskStatusId(request.ListId, request.StatusId, cancellationToken);

        var task = ProjectTask.Create(
            projectListId: request.ListId,
            name: request.Name,
            description: request.Description,
            customization: null,
            creatorId: CurrentUserId,
            statusId: statusId,
            priority: request.Priority,
            orderKey: list.GetNextItemOrderAndIncrement(),
            startDate: request.StartDate,
            dueDate: request.DueDate,
            storyPoints: request.StoryPoints,
            timeEstimate: request.TimeEstimate
        );

        await UnitOfWork.Set<ProjectTask>().AddAsync(task, cancellationToken);

        // Immediate assignment
        var assignees = new List<AssigneeDto>();
        if (request.AssigneeIds?.Any() == true)
        {
            // Validate assignees are workspace members and get their MemberIds
            var validUserIds = await ValidateWorkspaceMembers(request.AssigneeIds, cancellationToken);
            var memberIds = await GetWorkspaceMemberIds(validUserIds, cancellationToken);

            // 2. Validate bubble-up access (Space/Folder/List privacy)
            var accessibleMemberIds = await GetAccessibleMemberIds(request.ListId, EntityLayerType.ProjectList, memberIds);

            if (accessibleMemberIds.Count != memberIds.Count)
            {
                throw new System.ComponentModel.DataAnnotations.ValidationException("One or more assignees do not have permission to access this List.");
            }

            // Create assignments
            var assignments = accessibleMemberIds.Select(memberId => TaskAssignment.Create(task.Id, memberId, CurrentUserId));

            task.AddAsignees(assignments.ToList());

            // Fetch assignee details for the DTO
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
            ProjectListId = task.ProjectListId,
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
