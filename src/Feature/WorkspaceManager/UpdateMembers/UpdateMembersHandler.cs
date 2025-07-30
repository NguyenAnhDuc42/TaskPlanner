using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;

namespace src.Feature.WorkspaceManager.UpdateMembers;

public class UpdateMembersHandler : IRequestHandler<UpdateMembersRequest, Result<string, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly IHierarchyRepository _hierarchyRepository;
    public UpdateMembersHandler(PlannerDbContext context, IHierarchyRepository hierarchyRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<string, ErrorResponse>> Handle(UpdateMembersRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceWithMembersByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<string, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }
        if (!request.MemberIds.Any() || request.MemberIds is null)
            return Result<string, ErrorResponse>.Failure(ErrorResponse.BadRequest("Member list cannot be empty", "please provide at least one member."));
        var validMemberIds = request.MemberIds.Where(workspace.HasMember).ToList();   
        if (!validMemberIds.Any())
            return Result<string, ErrorResponse>.Failure(ErrorResponse.BadRequest("No valid members found to update", "please provide valid member IDs."));
        workspace.UpdateMembersRole(validMemberIds, request.Role);

        await _context.SaveChangesAsync(cancellationToken);
        
        var skippedCount = request.MemberIds.Count - validMemberIds.Count;
        var message = skippedCount > 0 
            ? $"Updated {validMemberIds.Count} members. Skipped {skippedCount} invalid members."
            : $"Updated {validMemberIds.Count} members successfully";
            
        return Result<string, ErrorResponse>.Success(message);

    }
}
