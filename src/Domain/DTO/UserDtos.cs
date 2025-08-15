using System;
using src.Domain.Enums;

namespace src.Domain.DTO;

public class AssigneeDto
{
    public Guid TaskId { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role? Role { get; set; }
}