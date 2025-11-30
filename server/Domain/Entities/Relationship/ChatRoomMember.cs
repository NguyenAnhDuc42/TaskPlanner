using Domain.Common;
using Domain.Entities.Support.Workspace;
using Domain.Enums.Workspace;

namespace Domain.Entities.Relationship;

public class ChatRoomMember : Composite
{
    public Guid ChatRoomId { get; private set; }
    public Guid UserId { get; private set; }
    public ChatRoomRole Role { get; private set; } = ChatRoomRole.Member;
    public bool IsMuted { get; private set; } = false;
    public DateTimeOffset? MuteEndTime { get; private set; }
    public bool IsBanned { get; private set; } = false;
    public DateTimeOffset? BannedAt { get; private set; }
    public Guid? BannedBy { get; private set; }
    public bool NotificationsEnabled { get; private set; } = true;
    public ChatRoom? ChatRoom { get; private set; }

    private ChatRoomMember() { }

    private ChatRoomMember(Guid chatRoomId, Guid userId, ChatRoomRole role = ChatRoomRole.Member, Guid creatorId)
    {
        ChatRoomId = chatRoomId;
        UserId = userId;
        Role = role;
        CreatorId = creatorId;
    }

    public static ChatRoomMember AddMember(Guid chatRoomId, Guid userId, ChatRoomRole role = ChatRoomRole.Member, Guid creatorId) =>
        new ChatRoomMember(chatRoomId, userId, role, creatorId);

    public static ChatRoomMember AddOwner(Guid chatRoomId, Guid userId, Guid creatorId) =>
        new ChatRoomMember(chatRoomId, userId, ChatRoomRole.Owner, creatorId);

    public static List<ChatRoomMember> AddMembers(Guid chatRoomId, List<Guid> userIds, ChatRoomRole role = ChatRoomRole.Member, Guid creatorId) =>
        userIds.Select(userId => new ChatRoomMember(chatRoomId, userId, role, creatorId)).ToList();

    public void MuteUntil(DateTimeOffset endTime)
    {
        if (endTime <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Mute end time must be in the future.", nameof(endTime));

        IsMuted = true;
        MuteEndTime = endTime;
        UpdateTimestamp();
    }

    public void UnMute()
    {
        IsMuted = false;
        MuteEndTime = null;
        UpdateTimestamp();
    }

    public void Ban(Guid bannedBy)
    {
        if (IsBanned)
            throw new InvalidOperationException("Member is already banned.");

        IsBanned = true;
        BannedAt = DateTimeOffset.UtcNow;
        BannedBy = bannedBy;
        UpdateTimestamp();
    }

    public void Unban()
    {
        if (!IsBanned)
            throw new InvalidOperationException("Member is not banned.");

        IsBanned = false;
        BannedAt = null;
        BannedBy = null;
        UpdateTimestamp();
    }

    public void PromoteToOwner()
    {
        Role = ChatRoomRole.Owner;
        UpdateTimestamp();
    }

    public void DemoteToMember()
    {
        Role = ChatRoomRole.Member;
        UpdateTimestamp();
    }
    public void TurnOffNotifications()
    {
        NotificationsEnabled = false;
        UpdateTimestamp();
    }

    public bool IsActive => !IsBanned && (MuteEndTime == null || MuteEndTime <= DateTimeOffset.UtcNow);
}