using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
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

        var space = Space.Create(request.name, request.workspaceId,userId);
        try
        {
            await _context.Spaces.AddAsync(space);
            await _context.SaveChangesAsync();
            return Result<CreateSpaceResponse, ErrorResponse>.Success(new CreateSpaceResponse(space.Id, "Space created successfully"));

        }
        catch (Exception ex)
        {
            return Result<CreateSpaceResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }
    }
}
