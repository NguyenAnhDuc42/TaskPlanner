using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.StatusManagement.SyncStatuses;
public record SyncStatusesCommand(
    Guid WorkflowId, 
    List<StatusSyncItem> Statuses,
    Guid? SpaceId = null,
    Guid? FolderId = null) : ICommandRequest;
    
public record StatusSyncItem(
    Guid? Id, 
    string Name, 
    string Color, 
    StatusCategory Category, 
    bool IsDeleted = false);


