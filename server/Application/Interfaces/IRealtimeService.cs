namespace Application.Interfaces;

public interface IRealtimeService
{
    Task NotifyWorkspaceAsync(Guid workspaceId, string eventName, object data, CancellationToken ct = default);

    Task NotifyUserAsync(Guid userId, string eventName, object data, CancellationToken ct = default);


}
