using System;
using Application.Interfaces.Repositories;
using MediatR;

namespace Application.Features.WorkspaceFeatures.MemberMange.RemoveMembers;

public class RemoveMembersHandler : IRequestHandler<RemoveMembersCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    public RemoveMembersHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public Task<Unit> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
