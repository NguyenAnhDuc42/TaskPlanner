namespace Application.Contract.UserContract;

public record class UserSummary
{
    public Guid Id { get; init; }
    public string Username { get; init; } = null!;
    public string Email { get; init; } = null!;
}
