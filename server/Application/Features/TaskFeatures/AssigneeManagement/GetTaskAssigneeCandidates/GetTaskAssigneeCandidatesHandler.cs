using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssigneeCandidates;

public class GetTaskAssigneeCandidatesHandler
    : BaseFeatureHandler, IRequestHandler<GetTaskAssigneeCandidatesQuery, List<TaskAssigneeCandidateDto>>
{
    public GetTaskAssigneeCandidatesHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<TaskAssigneeCandidateDto>> Handle(
        GetTaskAssigneeCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId);
        await EnsureCurrentUserCanAccessTask(task, cancellationToken);

        var allWorkspaceMemberIds = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => wm.ProjectWorkspaceId == WorkspaceId && wm.DeletedAt == null)
            .Select(wm => wm.Id)
            .ToListAsync(cancellationToken);

        var accessibleMemberIds = await GetAccessibleMemberIds(
            task.ProjectListId,
            EntityLayerType.ProjectList,
            allWorkspaceMemberIds);

        if (accessibleMemberIds.Count == 0)
        {
            return new List<TaskAssigneeCandidateDto>();
        }

        var assignedUserIds = (await UnitOfWork.QueryAsync<Guid>(@"
            SELECT wm.user_id
            FROM task_assignments ta
            JOIN workspace_members wm ON ta.workspace_member_id = wm.id
            WHERE ta.task_id = @TaskId
              AND ta.deleted_at IS NULL
              AND wm.deleted_at IS NULL", new { TaskId = task.Id }, cancellationToken)).ToArray();

        var safeLimit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, 100);

        var candidates = await UnitOfWork.QueryAsync<TaskAssigneeCandidateDto>(@"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.id = ANY(@WorkspaceMemberIds)
              AND wm.deleted_at IS NULL
              AND (@Search IS NULL OR u.name ILIKE ('%' || @Search || '%'))
              AND (array_length(@AssignedUserIds, 1) IS NULL OR NOT (u.id = ANY(@AssignedUserIds)))
            ORDER BY u.name
            LIMIT @Limit", new
        {
            WorkspaceMemberIds = accessibleMemberIds.ToArray(),
            Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
            AssignedUserIds = assignedUserIds,
            Limit = safeLimit
        }, cancellationToken);

        return candidates.ToList();
    }

    private async Task EnsureCurrentUserCanAccessTask(ProjectTask task, CancellationToken cancellationToken)
    {
        if (task.CreatorId == CurrentUserId)
        {
            return;
        }

        var currentWorkspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        var accessibleCurrentMemberIds = await GetAccessibleMemberIds(
            task.ProjectListId,
            EntityLayerType.ProjectList,
            new List<Guid> { currentWorkspaceMemberId });

        if (accessibleCurrentMemberIds.Count == 0)
        {
            throw new ValidationException("You do not have permission to view assignee candidates for this task.");
        }
    }
}
