using System;
using Domain.Common.Interfaces;

namespace Domain.Events.Workspace;

public class CreatedWorkspaceEvent : IDomainEvent
{
    public Guid EventId => 

    public DateTimeOffset OccurredOn => throw new NotImplementedException();

    public Guid? AggregateId => throw new NotImplementedException();
}
