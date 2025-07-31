using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using Microsoft.EntityFrameworkCore;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;


namespace src.Feature.FolderManager.CreateFolder;

public class CreateFolderHandler : IRequestHandler<CreateFolderRequest, Result<CreateFolderResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateFolderHandler(PlannerDbContext context, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateFolderResponse, ErrorResponse>> Handle(CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();

        // First, verify that the parent space exists within the specified workspace to ensure data integrity.
        var spaceExists = await _context.Spaces.AnyAsync(s => s.Id == request.spaceId && s.WorkspaceId == request.workspaceId, cancellationToken);
        if (!spaceExists)
        {
            return Result<CreateFolderResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Parent space not found in the specified workspace."));
        }

        var folder = PlanFolder.Create(request.name, request.workspaceId, request.spaceId, userId);

        await _context.Folders.AddAsync(folder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<CreateFolderResponse, ErrorResponse>.Success(new CreateFolderResponse(folder.Id, "Folder created successfully"));
    }
}