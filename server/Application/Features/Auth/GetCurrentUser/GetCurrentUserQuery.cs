using MediatR;
using Application.Contract.UserContract;

namespace Application.Features.Auth.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto>;
