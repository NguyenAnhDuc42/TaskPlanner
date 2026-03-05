using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public class DeleteTaskHandler : BaseFeatureHandler, IRequestHandler<DeleteTaskCommand, Unit>
{
    public DeleteTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId);
        var currentWorkspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        var accessibleCurrentMemberIds = await GetAccessibleMemberIds(
            task.ProjectListId,
            EntityLayerType.ProjectList,
            new List<Guid> { currentWorkspaceMemberId });

        if (accessibleCurrentMemberIds.Count == 0)
        {
            throw new ValidationException("You do not have permission to delete this task.");
        }

        task.SoftDelete();

        return Unit.Value;
    }
}
