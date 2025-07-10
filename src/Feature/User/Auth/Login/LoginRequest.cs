using MediatR;
using src.Helper.Results;

namespace src.Feature.User.Auth.Login;

public record LoginRequest(string email,string password) : IRequest<Result<LoginResponse, ErrorResponse>>;

