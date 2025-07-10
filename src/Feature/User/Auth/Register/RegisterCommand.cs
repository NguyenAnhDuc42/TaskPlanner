using MediatR;
using src.Helper.Results;

namespace src.Feature.User.Auth.Register;

public record RegisterCommand(string username,string email,string password) : IRequest<Result<RegisterResponse, ErrorResponse>>;

