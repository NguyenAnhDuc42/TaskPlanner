using System;
using src.Domain.Enums;

namespace src.Infrastructure.Abstractions.IServices;

public interface IPermissionService
{
   Task<Role> GetUserRole(Guid userId,Guid workspaceId);
}
