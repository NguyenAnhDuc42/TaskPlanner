using System;
using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Support.Notification;

public class NotificationEvent : Entity
{
    public Guid WorkspaceId { get; private set; } //filter stuff
    public Guid SourceId { get; private set; }
    public EntityType SourceType { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }  // the user that triggered the event
    public string? Payload { get; private set; } // message data
    public bool IsCritical { get; private set; } = false;
    public string AggregationKey { get; private set; } = string.Empty;
    public bool IsAggregated { get; private set; } = false;
    public Guid? AggregatedIntoEventId { get; private set; }  

    private NotificationEvent() { }

    private NotificationEvent(Guid workspaceId, Guid sourceId, EntityType sourceType, string eventType)
    {
        WorkspaceId = workspaceId;
        SourceId = sourceId;
        SourceType = sourceType;
        EventType = eventType;

    }
}
