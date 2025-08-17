using System;
using src.Domain.Enums;

namespace src.Domain.DTO;

public record TaskDto(Guid Id,string Name,DateTime? DueDate,Priority Priority,DateTime CreatedAt,DateTime UpdatedAt);



public record TasksMetadataDto (long OverdueCount, string PriorityBreakdownJson);

