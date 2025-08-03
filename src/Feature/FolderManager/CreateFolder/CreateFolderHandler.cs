using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using Microsoft.EntityFrameworkCore;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Feature.FolderManager.CreateFolder;

public class CreateFolderHandler : IRequestHandler<CreateFolderRequest, Result<CreateFolderResponse, ErrorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateFolderResponse, ErrorResponse>> Handle(CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();

        // First, verify that the parent space exists within the specified workspace to ensure data integrity.
        var spaceExists = await _unitOfWork.Spaces.GetByIdAsync(request.spaceId);
        if (spaceExists is null)
        {
            return Result<CreateFolderResponse, ErrorResponse>.Failure(ErrorResponse.NotFound("Parent space not found in the specified workspace."));
        }

        var folder = PlanFolder.Create(request.name, request.workspaceId, request.spaceId, userId);

        _unitOfWork.Folders.Add(folder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<CreateFolderResponse, ErrorResponse>.Success(new CreateFolderResponse(folder.Id, "Folder created successfully"));
    }
}