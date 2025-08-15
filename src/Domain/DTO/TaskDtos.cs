using System;
using src.Domain.Enums;

namespace src.Domain.DTO;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
