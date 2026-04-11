using Application.Common.Interfaces;

namespace Application.Features.Auth.Register;

public record class RegisterCommand(string username, string email, string password) : ICommandRequest<RegisterResponse>;
