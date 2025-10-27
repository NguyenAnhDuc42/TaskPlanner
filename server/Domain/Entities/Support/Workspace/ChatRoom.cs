using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums.Workspace;

namespace Domain.Entities.Support.Workspace;

public class ChatRoom : Entity
{
    public string Name { get; private set; } = null!;
    public Guid WorkspaceId { get; private set; }
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

    private ChatRoom(string name, Guid workspaceId, Guid creatorId, bool isPrivate = false, string? avatarUrl = null)
    {
        Name = name;
        WorkspaceId = workspaceId;
        CreatorId = creatorId;
        Type = ChatRoomType.PublicGroup;
        IsPrivate = isPrivate;
        AvatarUrl = avatarUrl;
    }

    public static ChatRoom Create(string name, Guid workspaceId, Guid creatorId, bool isPrivate = false, string? avatarUrl = null)
    {
        var chatRoom = new ChatRoom(name, workspaceId, creatorId, isPrivate, avatarUrl);

        // Add creator as owner
        var ownerMember = ChatRoomMember.AddOwner(chatRoom.Id, creatorId);
        chatRoom._members.Add(ownerMember);

        // chatRoom.AddDomainEvent(new ChatRoomCreatedEvent(chatRoom.Id, workspaceId, creatorId));
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

    public bool CanUserEdit(Guid userId, ChatRoomRole userRole, bool isWorkspaceOwner) =>
        userId == CreatorId ||
        userRole == ChatRoomRole.Owner ||
        isWorkspaceOwner;

    public void UpdateName(string newName, Guid userId, ChatRoomRole userRole, bool isWorkspaceOwner)
    {
        if (!CanUserEdit(userId, userRole, isWorkspaceOwner))
            throw new UnauthorizedAccessException("User cannot edit this chat room.");

        Name = newName;
        UpdateTimestamp();
        // AddDomainEvent(new ChatRoomUpdatedEvent(Id, WorkspaceId, userId));
    }

    public void UpdateAvatar(string? avatarUrl, Guid userId, ChatRoomRole userRole, bool isWorkspaceOwner)
    {
        if (!CanUserEdit(userId, userRole, isWorkspaceOwner))
            throw new UnauthorizedAccessException("User cannot edit this chat room.");

        AvatarUrl = avatarUrl;
        UpdateTimestamp();
    }
}