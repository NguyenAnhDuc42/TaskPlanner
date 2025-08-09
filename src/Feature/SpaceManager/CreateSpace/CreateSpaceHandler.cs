using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using Microsoft.EntityFrameworkCore;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Feature.SpaceManager.CreateSpace;

public class CreateSpaceHandler : IRequestHandler<CreateSpaceRequest, Result<CreateSpaceResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    public CreateSpaceHandler(PlannerDbContext context, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }
    public async Task<Result<CreateSpaceResponse, ErrorResponse>> Handle(CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == request.workspaceId, cancellationToken);
        if (!workspaceExists)
        {
            return Result<CreateSpaceResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }

        var space = Space.Create(request.name,request.icon,request.color, request.workspaceId, userId);
        await _context.Spaces.AddAsync(space, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<CreateSpaceResponse, ErrorResponse>.Success(new CreateSpaceResponse(space.Id, "Space created successfully"));
    }
}
