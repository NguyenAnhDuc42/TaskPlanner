using System;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IRequirePermission
{
    Guid? EntityId { get; }
    EntityType EntityType { get; }
    Permission RequiredPermission { get; }
}