using System;
using System.Security.Cryptography.X509Certificates;
using Domain.Common;
using Domain.Enums.Workspace;

namespace Domain.Entities.Support.Workspace;

public class ChatRoom : Entity
{
    public string Name { get; private set; } = null!;
    public Guid WorkspaceId { get; private set; }
    public ChatRoomType Type { get; private set; } = ChatRoomType.PublicGroup;
    public string? AvatarUrl { get; private set; } = null;
    public Guid CreatorId { get; private set; }

    private ChatRoom() { }
    private ChatRoom(string name, Guid workspaceId, ChatRoomType type, Guid creatorId, string? avatarUrl = null)
    {
        Name = name;
        WorkspaceId = workspaceId;
        Type = type;
        CreatorId = creatorId;
        AvatarUrl = avatarUrl;
    }

    public static ChatRoom Create(string name, Guid workspaceId, Guid creatorId, string? avatarUrl = null) =>
        new ChatRoom(name, workspaceId, ChatRoomType.PublicGroup, creatorId, avatarUrl);

}
