using Application.Common;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

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
    public async Task<Guid> Handle(AddMembersCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.members
            .Select(m => new { Email = m.email.Trim().ToLowerInvariant(), m.role })
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.Email)
            .Select(g => g.First())
            .ToList();
        var email = normalized.Select(m => m.Email).ToList();

        var users = await _unitOfWork.Set<User>()
            .Where(u => email.Contains(u.Email.ToLower()))
            .ToListAsync(cancellationToken);

        var usersByEmail = users.ToDictionary(u => u.Email.Trim().ToLower());

        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId) 
            ?? throw new KeyNotFoundException("No Workspace fouded");

        var existingMemberIds = workspace.Members.Select(m => m.UserId).ToHashSet();

        var specs = new List<(Guid UserId, Role Role, MembershipStatus Status, string? JoinMethod)>();

        foreach (var member in normalized)
        {
            if (!usersByEmail.TryGetValue(member.Email, out var user)) continue;
            if (existingMemberIds.Contains(user.Id)) continue;
            specs.Add((user.Id, member.role, MembershipStatus.Active, "Invite"));
        }
        if (specs.Count > 0)
        {
            workspace.AddMembers(specs,_currentUserService.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return workspace.Id;
    }
}

