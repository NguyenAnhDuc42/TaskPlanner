using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public enum ItemType
{
    Space,
    Folder,
    Task
}

public record class MoveItemCommand(
    Guid ItemId,
    ItemType ItemType,
    Guid? TargetParentId,  // New parent (Space for Folder, Folder/Space for Task)
    string? PreviousItemOrderKey,  // OrderKey of item above (null if moving to top)
    string? NextItemOrderKey       // OrderKey of item below (null if moving to bottom)
) : ICommand<Unit>;
