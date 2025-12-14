using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

public enum ItemType
{
    Space,
    Folder,
    List
}

public record class MoveItemCommand(
    Guid ItemId,
    ItemType ItemType,
    Guid? TargetParentId,  // New parent (Space for Folder, Folder for List)
    long? PreviousItemOrderKey,  // OrderKey of item above (null if moving to top)
    long? NextItemOrderKey       // OrderKey of item below (null if moving to bottom)
) : ICommand<Unit>;
