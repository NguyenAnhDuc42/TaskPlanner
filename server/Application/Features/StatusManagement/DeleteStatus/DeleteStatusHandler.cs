using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Support;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.DeleteStatus;

public class DeleteStatusHandler : BaseFeatureHandler, IRequestHandler<DeleteStatusCommand, Unit>
{
    public DeleteStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);
        status.SoftDelete();
        
        return Unit.Value;
    }
}
