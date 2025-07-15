using MediatR;
using src.Helper.Results;

namespace src.Feature.FolderManager.CreateFolder;

public record class CreateFolderRequest(Guid workspaceId, Guid spaceId, string name) : IRequest<Result<CreateFolderResponse, ErrorResponse>>;