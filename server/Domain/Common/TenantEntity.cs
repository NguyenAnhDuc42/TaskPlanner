using System;
using Domain.Entities;

namespace Domain.Common;

public interface ITenanted
{
    Guid ProjectWorkspaceId { get; }
}

public abstract class TenantEntity : Entity, ITenanted
{
    public Guid ProjectWorkspaceId { get; protected set; }

    protected TenantEntity() { }

    protected TenantEntity(Guid id, Guid projectWorkspaceId) : base(id)
    {
        ProjectWorkspaceId = projectWorkspaceId;
    }
}
