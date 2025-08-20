using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Application.Common.DTOs;

public record ListSummary(Guid Id,string Name);


public record StatusColumn(Guid StatusId, string Name, string Color, StatusType Type, List<TaskSummary> Tasks);

// Represents a node in hierarchical data structure