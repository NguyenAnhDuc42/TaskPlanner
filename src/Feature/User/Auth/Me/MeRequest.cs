using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;

namespace src.Feature.User.Auth.Me;

public record MeRequest() : IRequest<Result<UserDetail, ErrorResponse>>; 