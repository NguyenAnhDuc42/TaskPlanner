using src.Domain.Enums;

namespace src.Contract;
public record UserDetail(Guid Id, string Name, string Email);
public record UserSummary(Guid Id, string Name, string Email,Role? Role);
public record UserBasic(Guid Id, string Name);