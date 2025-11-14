using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums.Workspace;

namespace Domain.Entities.Support.Workspace;

public class ChatRoom : Entity
{
    public string Name { get; private set; } = null!;
    public Guid ProjectWorkspaceId { get; private set; }
    public ChatRoomType Type { get; private set; } = ChatRoomType.PublicGroup;
    public string? AvatarUrl { get; private set; }
    public Guid CreatorId { get; private set; }
    public bool IsPrivate { get; private set; } = false;
    public bool IsArchived { get; private set; } = false;

    // Navigation properties
    private readonly List<ChatMessage> _messages = new();
    private readonly List<ChatRoomMember> _members = new();

    public IReadOnlyCollection<ChatMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<ChatRoomMember> Members => _members.AsReadOnly();

    private ChatRoom() { }

    private ChatRoom(string name, Guid projectWorkspaceId, Guid creatorId, bool isPrivate = false, string? avatarUrl = null)
    {
        Name = name;
        ProjectWorkspaceId = projectWorkspaceId;
        CreatorId = creatorId;
        Type = ChatRoomType.PublicGroup;
        IsPrivate = isPrivate;
        AvatarUrl = avatarUrl;
    }

    public static ChatRoom Create(string name, Guid projectWorkspaceId, Guid creatorId, bool isPrivate = false, string? avatarUrl = null)
    {
        var chatRoom = new ChatRoom(name, projectWorkspaceId, creatorId, isPrivate, avatarUrl);

        // Add creator as owner
        var ownerMember = ChatRoomMember.AddOwner(chatRoom.Id, creatorId);
        chatRoom._members.Add(ownerMember);

        return chatRoom;
    }

    public void AddMember(ChatRoomMember member)
    {
        if (_members.Any(m => m.UserId == member.UserId && m.ChatRoomId == Id))
            throw new InvalidOperationException("User is already a member of this chat room.");

        _members.Add(member);
        UpdateTimestamp();
        // AddDomainEvent(new MemberAddedToChatRoomEvent(Id, member.UserId, member.Role));
    }

    public void AddMembers(List<ChatRoomMember> members)
    {
        foreach (var member in members)
        {
            AddMember(member);
        }
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this chat room.");

        _members.Remove(member);
        UpdateTimestamp();
        // AddDomainEvent(new MemberRemovedFromChatRoomEvent(Id, userId));
    }

    public void RemoveMembers(List<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            RemoveMember(userId);
        }
    }

    public void AddMessage(ChatMessage message)
    {
        if (message.ChatRoomId != Id)
            throw new InvalidOperationException("Message does not belong to this chat room.");

        _messages.Add(message);
        UpdateTimestamp();
    }

    public void ArchiveRoom()
    {
        if (IsArchived)
            throw new InvalidOperationException("Chat room is already archived.");

        IsArchived = true;
        UpdateTimestamp();
        // AddDomainEvent(new ChatRoomArchivedEvent(Id, WorkspaceId));
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public ChatRoomRole? GetMemberRole(Guid userId) => _members.FirstOrDefault(m => m.UserId == userId)?.Role;

    public bool CanUserDelete(Guid userId, ChatRoomRole userRole, bool isWorkspaceOwner) =>
        userId == CreatorId ||                    // Creator can always delete
        userRole == ChatRoomRole.Owner ||         // Room owner can delete
        isWorkspaceOwner;                         // Workspace owner can delete any room

    public void Update(string? name = null, string? avatarUrl = null, bool? isPrivate = null, bool? isArchived = null)
    {
        var changed = false;

        if (name is not null)
        {
            var trimmedName = name.Trim();
            if (trimmedName == string.Empty)
            {
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            }
            if (trimmedName != Name)
            {
                Name = trimmedName;
                changed = true;
            }
        }

        if (avatarUrl is not null)
        {
            var candidateUrl = string.IsNullOrWhiteSpace(avatarUrl.Trim()) ? null : avatarUrl.Trim();
            if (candidateUrl != AvatarUrl)
            {
                AvatarUrl = candidateUrl;
                changed = true;
            }
        }

        if (isPrivate.HasValue && isPrivate.Value != IsPrivate)
        {
            IsPrivate = isPrivate.Value;
            changed = true;
        }

        if (isArchived.HasValue && isArchived.Value != IsArchived)
        {
            IsArchived = isArchived.Value;
            changed = true;
            // domaineventexample
            // if (isArchived.Value) AddDomainEvent(new ChatRoomArchivedEvent(Id, WorkspaceId));
            // else AddDomainEvent(new ChatRoomUnarchivedEvent(Id, WorkspaceId));
        }

        if (changed)
        {
            UpdateTimestamp();
            // AddDomainEvent(new ChatRoomUpdatedEvent(Id, WorkspaceId));
        }
    }
}