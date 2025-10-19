using System;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.Relationship;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberMange.AddMembers;

public class AddMembersHandler : IRequestHandler<AddMembersCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }
    public async Task<Unit> Handle(AddMembersCommand command, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
        var emails = command.members.Select(m => m.email).ToList();
        var users = await _unitOfWork.Set<User>().Where(u => emails.Contains(u.Email)).AsNoTracking().ToListAsync(cancellationToken);
        var memberSpecs = command.members
            .Join(users, m => m.email, u => u.Email, (m, u) => (u.Id, m.role, m.status, m.joinMethod))
            .ToList();

        var members = WorkspaceMember.AddBulk(memberSpecs, command.workspaceId,currentUserId);
        await _unitOfWork.Set<WorkspaceMember>().AddRangeAsync(members, cancellationToken);

        return Unit.Value;

    }
}
