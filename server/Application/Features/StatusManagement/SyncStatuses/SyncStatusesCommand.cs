using Application.Common.Interfaces;
using System;
using Domain.Enums;
using MediatR;
using Domain.Enums.RelationShip;

namespace Application.Features.StatusManagement.SyncStatuses;

public record StatusSyncItem(
    Guid? Id, 
    string Name, 
    string Color, 
    StatusCategory Category, 
    bool IsDeleted = false);

public record SyncStatusesCommand(
    Guid LayerId, 
    EntityLayerType LayerType, 
    List<StatusSyncItem> Statuses) : ICommand<Unit>;
