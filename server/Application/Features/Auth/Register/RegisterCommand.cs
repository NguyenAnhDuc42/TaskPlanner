using Application.Common.Interfaces;

namespace Application.Features.Auth.Register;

public record RegisterCommand(string username, string email, string password) : ICommandRequest<RegisterResponse>;
