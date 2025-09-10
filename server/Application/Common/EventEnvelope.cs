namespace Application.Common;

public record EventEnvelope<TPayload>(
        Guid WorkspaceId,
        string EventName,
        TPayload Payload,
        Guid ActorId,
        DateTime OccurredAt,
        int Version = 1,
        string? ParentType = null,
        Guid? ParentId = null
    );