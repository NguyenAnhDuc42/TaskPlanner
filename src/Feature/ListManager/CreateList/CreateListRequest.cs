using MediatR;
using src.Helper.Results;

namespace src.Feature.ListManager.CreateList;

public record class CreateListRequest(Guid workspaceId, Guid spaceId, Guid? folderId, string name) : IRequest<Result<CreateListResponse, ErrorResponse>>;
