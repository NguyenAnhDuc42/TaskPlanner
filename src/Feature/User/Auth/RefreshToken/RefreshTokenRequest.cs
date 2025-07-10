using MediatR;
using src.Helper.Results;

namespace src.Feature.User.Auth.RefreshToken;

public record RefreshTokenRequest() : IRequest<Result<RefreshTokenResponse, ErrorResponse>>;

