using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.StatusManagement.SyncStatuses;

public record StatusSyncItem(
    Guid? Id, 
    string Name, 
    string Color, 
    StatusCategory Category, 
    bool IsDeleted = false);

public record SyncStatusesCommand(
    Guid WorkflowId, 
    List<StatusSyncItem> Statuses) : ICommand<Unit>;
