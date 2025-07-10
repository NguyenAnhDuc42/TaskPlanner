using MediatR;
using src.Helper.Results;

namespace src.Feature.User.Auth.Logout;
public record LogoutRequest() : IRequest<Result<LogoutResponse, ErrorResponse>>;