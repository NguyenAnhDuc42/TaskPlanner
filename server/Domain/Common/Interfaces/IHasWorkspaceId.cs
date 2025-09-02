using System;

namespace Domain.Common.Interfaces;

public interface IHasWorkspaceId
{
    Guid ProjectWorkspaceId { get; }
}
