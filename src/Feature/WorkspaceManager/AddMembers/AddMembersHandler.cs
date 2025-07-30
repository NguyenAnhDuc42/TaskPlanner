using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;

namespace src.Feature.WorkspaceManager.AddMembers;

public class AddMembersHandler : IRequestHandler<AddMembersRequest, Result<AddMembersResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly IUserRepository _userRepository;
    public AddMembersHandler(PlannerDbContext context,IHierarchyRepository hierarchyRepository, IUserRepository userRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }
    public async Task<Result<AddMembersResponse, ErrorResponse>> Handle(AddMembersRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<AddMembersResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }

        if (request.emails is null || !request.emails.Any())
            return Result<AddMembersResponse, ErrorResponse>.Failure(ErrorResponse.BadRequest("Email list cannot be empty","please provide at least one email."));

        var users = await _userRepository.GetUsersByEmailsAsync(request.emails, cancellationToken); 
        var addedEmails = new List<string>();
        foreach (var user in users)
        {
            workspace.AddMember(user.Id, request.role);
            addedEmails.Add(user.Email);
        }

        if (!addedEmails.Any())
            return Result<AddMembersResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("No valid users found"));

        await _context.SaveChangesAsync(cancellationToken);
        return Result<AddMembersResponse, ErrorResponse>.Success(new AddMembersResponse(addedEmails, "Members added successfully"));
       
    }   
}
