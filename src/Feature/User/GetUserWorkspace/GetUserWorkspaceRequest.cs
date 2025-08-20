using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;

namespace src.Feature.User.GetUserWorkspace;

public record class GetUserWorkspaceRequest() : IRequest<Result<List<WorkspaceDetail>,ErrorResponse>>;
