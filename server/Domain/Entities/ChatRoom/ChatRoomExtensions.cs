namespace Domain.Entities;

public static class ChatRoomExtensions
{
    public static IQueryable<ChatRoom> ById(this IQueryable<ChatRoom> query, Guid id)
        => query.Where(cr => cr.Id == id);

    public static IQueryable<ChatRoom> ByWorkspace(this IQueryable<ChatRoom> query, Guid workspaceId)
        => query.Where(cr => cr.ProjectWorkspaceId == workspaceId);
}
