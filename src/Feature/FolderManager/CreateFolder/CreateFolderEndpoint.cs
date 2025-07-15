using Microsoft.AspNetCore.Mvc;
using src.Feature.FolderManager.CreateFolder;
using src.Helper.Results;

namespace src.Feature.FolderManager;

public partial class FolderController
{
    [HttpPost]
    public async Task<IActionResult> CreateFolder(CreateFolderRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}