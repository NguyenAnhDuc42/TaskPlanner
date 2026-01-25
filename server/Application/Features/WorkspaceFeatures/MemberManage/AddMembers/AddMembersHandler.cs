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
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler : IRequestHandler<AddMembersCommand, Guid>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;

    public AddMembersHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, HybridCache cache)
    {
       _currentUserService = currentUserService;
       _unitOfWork = unitOfWork;
       _cache = cache;
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

        // Fetch ALL members including soft-deleted ones to check for re-adds
        var existingMembers = await _unitOfWork.Set<WorkspaceMember>()
            .IgnoreQueryFilters()
            .Where(wm => wm.ProjectWorkspaceId == request.workspaceId)
            .ToListAsync(cancellationToken);

        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .FirstOrDefaultAsync(w => w.Id == request.workspaceId) 
            ?? throw new KeyNotFoundException("No Workspace fouded");

        var specs = new List<(Guid UserId, Role Role, MembershipStatus Status, string? JoinMethod)>();

        foreach (var member in normalizedMembers)
        {
            if(!usersByNormalizedEmail.TryGetValue(member.NormalizedEmail, out var user)) continue;

            var existing = existingMembers.FirstOrDefault(m => m.UserId == user.Id);
            if (existing != null)
            {
                if (existing.DeletedAt != null)
                {
                    // Restore soft-deleted member
                    existing.UpdateRole(member.role);
                    existing.RestoreMember();
                }
                continue;
            }

            specs.Add((user.Id, member.role, MembershipStatus.Active, "Invite"));
        }
        if (specs.Count > 0)
        {
            workspace.AddMembers(specs,_currentUserService.UserId);
            await _cache.RemoveByTagAsync($"workspaces:{request.workspaceId}:members", cancellationToken);
        }
        var entries = _unitOfWork.ChangeTracker.Entries();
        Console.WriteLine($"Tracked entries: {entries.Count()}");
        foreach (var entry in entries)
        {
            Console.WriteLine($"  Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
        }

        return workspace.Id;
    }
}

