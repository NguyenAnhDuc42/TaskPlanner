using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.Notification;

namespace Domain.Entities.Support.Notification;

public class NotificationPreference : Entity
{ 
    public Guid UserId { get; private set; }
    public ScopeType ScopeType { get; private set; }
    public Guid? ScopeId { get; private set; } // global scope has null ScopeId
    public string EnabledEventTypes { get; private set; } = string.Empty; // Comma-separated list of enabled event types
    public NotificationFrequency NotificationFrequency { get; private set; } = NotificationFrequency.Instant;
    public NotificationChanel NotificationChannels { get; private set; } = NotificationChanel.Email | NotificationChanel.InApp;
    public bool IsMuted { get; private set; } = false;

    private NotificationPreference() { } // EF
    private NotificationPreference(Guid userId, ScopeType scopeType, Guid? scopeId)
    {
        UserId = userId;
        ScopeType = scopeType;
        ScopeId = scopeId;
    }
    public static NotificationPreference Create(Guid userId, ScopeType scopeType, Guid? scopeId) =>
    new NotificationPreference(userId, scopeType, scopeId);

}   
