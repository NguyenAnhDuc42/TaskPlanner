using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using src.Feature.SpaceManager.GetSpaceInfo;
using src.Helper.Results;

namespace src.Feature.SpaceManager;

public partial class SpaceController
{
    [HttpGet("{spaceId}/tasks")]
    public async Task<IActionResult> GetSpaceTasks(Guid spaceId, CancellationToken cancellationToken)
    {
        var request = new GetSpaceInfoRequest(spaceId);
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    }
}
