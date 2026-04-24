using Application.Features.TaskFeatures;
using Domain.Enums;

namespace Application.Features.ViewFeatures;

public record ViewDataResponse(
    Guid ViewId,
    ViewType ViewType,
    object Data
);

public record TaskViewData(
    List<ExplorerStatusGroupDto> Groups
);

public record AssetViewData(
    List<AssetItemDto> Items,
    int TotalCount
);

public record OverviewViewData(
    Guid Id,
    string Name,
    string? Description,
    Guid? StatusId,
    Guid? WorkflowId,
    Guid? ChatRoomId,
    Guid CreatorId,
    DateTimeOffset CreatedAt,
    OverviewStats Stats // Still useful to keep stats in the overview panel
);

public record OverviewStats(
    int TotalTasks,
    int TotalFolders
);

public record AssetItemDto(
    Guid Id,
    string Name,
    string? Type, // "Document", "File", "Media", "Link"
    string? Extension,
    long? SizeBytes,
    Guid CreatorId,
    DateTimeOffset CreatedAt
);

public record FolderItemDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    Guid? WorkflowId = null,
    Guid? StatusId = null
);

public record TaskItemDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    Guid? StatusId = null,
    Priority? Priority = null,
    DateTimeOffset? DueDate = null
);

public record ExplorerStatusGroupDto(
    Guid StatusId,
    string StatusName,
    StatusCategory Category,
    string Color,
    List<FolderItemDto> Folders,
    List<TaskItemDto> Tasks
);
