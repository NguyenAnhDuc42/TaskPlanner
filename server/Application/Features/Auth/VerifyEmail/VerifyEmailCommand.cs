using MediatR;

namespace Application.Features.Auth.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest;
