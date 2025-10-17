using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspace;

public class UpdateWorkspaceHandler : IRequestHandler<UpdateWorkspaceCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    public UpdateWorkspaceHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {

        var workspace = await _unitOfWork.Set<ProjectWorkspace>().FindAsync(request.Id, cancellationToken);
        if (workspace == null)
        {
            throw new Exception("Workspace not found");
        }


        workspace.Update(
            name: request.Name,
            description: request.Description,
            color: request.Color,
            icon: request.Icon,
            theme: request.Theme,
            variant: request.Variant,
            strictJoin: request.StrictJoin,
            isArchived: request.IsArchived,
            regenerateJoinCode: request.RegenerateJoinCode
        );

        _unitOfWork.Set<ProjectWorkspace>().Update(workspace);

        await _unitOfWork.CommitTransactionAsync(cancellationToken);
        return Unit.Value;
    }

}
