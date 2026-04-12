using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public record MoveItemCommand(
    Guid ItemId,
    EntityLayerType ItemType,
    Guid? TargetParentId,           // New parent (Space for Folder, Folder/Space for Task)
    string? PreviousItemOrderKey,   // OrderKey of item above
    string? NextItemOrderKey,        // OrderKey of item below
    string? NewOrderKey             // Optional: Pre-calculated key from frontend
) : ICommandRequest, IAuthorizedWorkspaceRequest;
