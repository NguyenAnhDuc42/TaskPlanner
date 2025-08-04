using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.User.Auth.Me;

public record MeRequest() : IRequest<Result<UserDetail, ErrorResponse>>; 