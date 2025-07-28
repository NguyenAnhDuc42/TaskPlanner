using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;

namespace src.Feature.WorkspaceManager.DeleteMembers;

public class DeleteMembersHandler : IRequestHandler<DeleteMembersRequest, Result<string, ErrorResponse>>
{
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly PlannerDbContext _context;
    public DeleteMembersHandler(IHierarchyRepository hierarchyRepository, PlannerDbContext context)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public async Task<Result<string, ErrorResponse>> Handle(DeleteMembersRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceWithMembersByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
            return Result<string, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        var validationResult = ValidateDeletion(workspace, request.memberIds);
        if (validationResult.IsSuccess == false)
            return validationResult;

        workspace.RemoveMembers(request.memberIds);

        _context.Workspaces.Update(workspace);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<string, ErrorResponse>.Success("Members deleted successfully");
    }

    private Result<string, ErrorResponse> ValidateDeletion(Workspace workspace, IEnumerable<Guid> memberIds)
    {

        foreach (var memberId in memberIds)
        {
            if (workspace.CreatorId == memberId)
                return Result<string, ErrorResponse>.Failure(ErrorResponse.BadRequest("Cannot delete workspace creator", "The creator of the workspace cannot be removed."));

            if (!workspace.HasMember(memberId))
                return Result<string, ErrorResponse>.Failure(ErrorResponse.NotFound($"User {memberId} not found in workspace"));
        }

        return Result<string, ErrorResponse>.Success(string.Empty);
    }
}

