using Application.Common.Interfaces;

namespace Application.Features.Auth.Login;

public record class LoginCommand(string email, string password) : ICommandRequest<LoginResponse>;