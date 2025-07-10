using MediatR;
using src.Helper.Results;

namespace src.Feature.User.Auth.Me;

public record MeRequest() : IRequest<Result<MeResponse, ErrorResponse>>; 