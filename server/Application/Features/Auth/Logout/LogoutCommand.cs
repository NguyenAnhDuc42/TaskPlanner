using MediatR;

namespace Application.Features.Auth.Logout;

public record class LogoutCommand() : IRequest<LogoutResponse>;
