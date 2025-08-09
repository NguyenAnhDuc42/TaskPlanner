using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;

using src.Domain.Enums;

namespace src.Domain.Entities.WorkspaceEntity;

public class Workspace : Agregate<Guid>
{

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string JoinCode { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }

    public ICollection<Space> Spaces { get; set; } = new List<Space>();
    public ICollection<UserWorkspace> Members { get; set; } = new List<UserWorkspace>();

    public Guid CreatorId { get; private set; }

    private Workspace() { }
    private Workspace(Guid id, string name, string description, string joinCode, string color,string icon, Guid creatorId, bool isPrivate) : base(id)
    {
        Name = name;
        Description = description;
        JoinCode = joinCode;
        Color = color;
        Icon = icon;
        CreatorId = creatorId;
        IsPrivate = isPrivate;
    }

    public static Workspace Create(string name, string description, string color,string icon, Guid creatorId, bool isPrivate)
    {
        var joinCode = GenerateRandomCode();
        var workspace = new Workspace(Guid.NewGuid(), name, description, joinCode, color,icon, creatorId, isPrivate);

        return workspace;
    }
    public static Workspace CreateSampleWorkspace(Guid creatorId)
    {
        var joinCode = GenerateRandomCode();
        var workspace = new Workspace(Guid.NewGuid(), "Sample Workspace", "This is a sample workspace", joinCode, "","", creatorId, isPrivate : false);
        return workspace;
    }

    public void AddSpace(Space space)
    {
        Spaces.Add(space);
        this.UpdateTimestamp();

    }

    public void AddMember(Guid userId, Role role)
    {
        if (Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is already a member of this workspace.");
        }

        var userWorkspace = new UserWorkspace(userId, Id, role);
        Members.Add(userWorkspace);
    }
    public void RemoveMember(Guid userId)
    { 
        if (!Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is not a member of this workspace.");
        }
        if (CreatorId == userId)
        {
            throw new InvalidOperationException("Cannot remove workspace creator");
        }
        var memberToRemove = Members.First(m => m.UserId == userId);
        Members.Remove(memberToRemove);
    }
    public bool HasMember(Guid userId)
    {
        return Members.Any(m => m.UserId == userId);
    }
    public void RemoveMembers(IEnumerable<Guid> memberIds)
    {
        var memberIdsSet = memberIds.ToHashSet();

        foreach (var memberId in memberIdsSet)
        {
            if (CreatorId == memberId)
                throw new InvalidOperationException("Cannot delete workspace creator");

            if (!Members.Any(m => m.UserId == memberId))
                throw new InvalidOperationException($"User {memberId} not found in workspace");
        }
        var membersToRemove = Members.Where(m => memberIdsSet.Contains(m.UserId)).ToList();
        foreach (var member in membersToRemove)
        {
            Members.Remove(member);
        }
    }
    public void UpdateMembersRole(IEnumerable<Guid> memberIds, Role newRole)
    {
        foreach (var memberId in memberIds)
        {
            if (CreatorId == memberId && newRole != Role.Owner)
                throw new InvalidOperationException("Workspace creator must remain Owner");

            var member = Members.First(m => m.UserId == memberId); 
            member.Role = newRole;
        }
    }

    public static string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
