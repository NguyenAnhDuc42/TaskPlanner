using Application.Features.Auth.Common;
using MediatR;

namespace Application.Features.Auth.Login;

public record class LoginCommand(string email, string password) : IRequest<LoginResponse>;