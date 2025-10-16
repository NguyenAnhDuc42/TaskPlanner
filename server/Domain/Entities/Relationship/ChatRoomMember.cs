using System;
using Domain.Common;
using Domain.Enums.Workspace;

namespace Domain.Entities.Relationship;

public class ChatRoomMember : Composite
{
    public Guid ChatRoomId { get; private set; }
    public Guid UserId { get; private set; }
    public ChatRoomRole Role { get; private set; } = ChatRoomRole.Member;
    public bool IsMuted { get; private set; } = false;
    public DateTimeOffset? MuteEndTime { get; private set; } = null;
    public bool IsBanned { get; private set; } = false;

    private ChatRoomMember() { }
    private ChatRoomMember(Guid chatRoomId, Guid userId, ChatRoomRole role = ChatRoomRole.Member)
    {
        ChatRoomId = chatRoomId;
        UserId = userId;
        Role = role;
    }
    public static ChatRoomMember AddMember(Guid chatRoomId, Guid userId, ChatRoomRole role = ChatRoomRole.Member) =>
        new ChatRoomMember(chatRoomId, userId, role);
    
}
