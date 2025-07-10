using src.Domain.Entities.UserEntity;

namespace src.Feature.User.Auth.Me;

public record MeResponse(string id, string name, string email, string? avatar);
