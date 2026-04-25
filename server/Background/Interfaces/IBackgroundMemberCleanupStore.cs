using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Background.Interfaces;

public interface IBackgroundMemberCleanupStore
{
    Task<List<Guid>> GetMemberIdsForUsersAsync(Guid workspaceId, IEnumerable<Guid> userIds);
    
    Task<(int EntityAccessDeleted, int AssignmentsDeleted)> CleanupMemberDataAsync(Guid workspaceId, List<Guid> memberIds);
}
