using System;
using Domain.Common.Interfaces;

namespace Domain.Events.Workspace;

public record CreatedWorkspaceEvent(Guid userId,Guid workspaceId) : BaseDomainEvent;

