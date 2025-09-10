using System;
using Application.Contract.WorkspaceContract;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.ProjectEntities;
using Mapster;
using MediatR;
using Microsoft.CodeAnalysis;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.CreateWrokspace;

public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceCommand, WorkspaceDetail>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    public CreateWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }
    public async Task<WorkspaceDetail> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) throw new UnauthorizedAccessException("User not authenticated");

        var workspace = ProjectWorkspace.Create(request.name, request.description, request.color, request.icon, currentUserId, request.visibility);
        await _unitOfWork.ProjectWorkspaces.AddAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = workspace.Adapt<WorkspaceDetail>();
        detail = detail with
        {
            CurrentRole = detail.Members.First(m => m.Id == currentUserId)
        };
        return detail;
    }
}
