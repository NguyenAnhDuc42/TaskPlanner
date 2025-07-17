using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using src.Infrastructure.Data;

namespace src.Feature.Workspace.CreateWorkspace;

public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceRequest, Result<CreateWorkspaceResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public CreateWorkspaceHandler(PlannerDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<Result<CreateWorkspaceResponse, ErrorResponse>> Handle(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Result<CreateWorkspaceResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "User not found."));
        }

        var workspace = Domain.Entities.WorkspaceEntity.Workspace.Create(request.Name, request.Description, request.Color, userId, request.IsPrivate);

        try
        {
            _context.Workspaces.Add(workspace);
            var saved = await _context.SaveChangesAsync(cancellationToken) > 0;

            if (saved)
            {
                return Result<CreateWorkspaceResponse, ErrorResponse>.Success(new CreateWorkspaceResponse(workspace.Id, "Workspace created successfully."));
            }

            return Result<CreateWorkspaceResponse, ErrorResponse>.Failure(ErrorResponse.Internal("Failed to save workspace to the database."));
        }
        catch (Exception ex)
        {
            return Result<CreateWorkspaceResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }
    }
}
