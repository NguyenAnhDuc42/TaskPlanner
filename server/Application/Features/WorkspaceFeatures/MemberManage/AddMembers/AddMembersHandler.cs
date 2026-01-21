
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : IRequestHandler<AddMembersCommand, Guid>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AddMembersHandler(ICurrentUserService currentUserService,IUnitOfWork unitOfWork)
    {
       _currentUserService = currentUserService;
       _unitOfWork = unitOfWork;
    }
    // future add email notification 
    public async Task<Guid> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        var normalizedMembers = request.members
            .DistinctBy(m => m.email.Trim().ToLowerInvariant())
            .Where(m => !string.IsNullOrWhiteSpace(m.email))
            .Select(m => new
            {
                NormalizedEmail = m.email.Trim().ToLowerInvariant(),
                m.role
            })
            .ToList();
        if (normalizedMembers.Count == 0) return request.workspaceId;

        var emailsToFind = normalizedMembers
            .Select(m => m.NormalizedEmail)
            .ToList();

        var users = await _unitOfWork.Set<User>()
            .Where(u => emailsToFind.Contains(u.Email.ToLower()))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var usersByNormalizedEmail = users.ToDictionary(
                keySelector: u => u.Email.Trim().ToLowerInvariant(),
                elementSelector: u => u);

        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId) 
            ?? throw new KeyNotFoundException("No Workspace fouded");

        var userIdsToCheck = users.Select(u => u.Id).ToList();

        var existingMemberIds = await _unitOfWork.Set<WorkspaceMember>()
            .Where(m => m.ProjectWorkspaceId == request.workspaceId &&
                        userIdsToCheck.Contains(m.UserId))
            .AsNoTracking()
            .Select(m => m.UserId)
            .ToHashSetAsync(cancellationToken);

        var specs = new List<(Guid UserId, Role Role, MembershipStatus Status, string? JoinMethod)>();


        foreach (var member in normalizedMembers)
        {
            if(!usersByNormalizedEmail.TryGetValue(member.NormalizedEmail, out var user)) continue;

            if (existingMemberIds.Contains(user.Id)) continue;

            specs.Add((user.Id, member.role, MembershipStatus.Active, "Invite"));
        }
        if (specs.Count > 0)
        {
            workspace.AddMembers(specs,_currentUserService.UserId);
        }

        return workspace.Id;
    }
}

