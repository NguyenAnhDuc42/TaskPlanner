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
        var emails = request.emails;
        if (emails == null || emails.Count == 0)
        {
            return Result<AddMembersResponse, ErrorResponse>.Failure(ErrorResponse.BadRequest("Email list cannot be empty.", "Please provide at least one email address."));
        }
        var addedEmails = new List<string>();
        foreach (var email in emails)
        {
            var user = await _userRepository.GetUserByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                continue;
            }
            if (user != null)
            {
                workspace.AddMember(user.Id, request.role);
                addedEmails.Add(email);
            }
        }
        if (addedEmails.Count == 0)
        {
            return Result<AddMembersResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("No valid users found.", "None of the provided emails correspond to existing users."));
        }

        _context.Workspaces.Update(workspace);
        var saveResult = await _context.SaveChangesAsync(cancellationToken);
        if (saveResult <= 0)
        {
            return Result<AddMembersResponse, ErrorResponse>.Failure(ErrorResponse.Internal("An error occurred while saving changes to the database."));
        }
        return Result<AddMembersResponse, ErrorResponse>.Success(new AddMembersResponse(addedEmails, "Members added successfully."));
    }   
}
