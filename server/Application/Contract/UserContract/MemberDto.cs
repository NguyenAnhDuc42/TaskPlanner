using Domain.Enums;

namespace Application.Contract.UserContract;

public record class MemberDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public Role Role { get; init; }
}
