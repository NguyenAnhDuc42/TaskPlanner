using MediatR;
using src.Helper.Results;

namespace src.Feature.FolderManager.GetFolderInfo;

public record class GetFolderInfoRequest(Guid folderId) : IRequest<Result<TaskList, ErrorResponse>>;