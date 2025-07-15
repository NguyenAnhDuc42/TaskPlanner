using System;
using Microsoft.AspNetCore.Mvc;
using src.Feature.SpaceManager.CreateSpace;
using src.Helper.Results;

namespace src.Feature.SpaceManager;

public partial class SpaceController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSpaceRequest request)
    {
        var result = await _mediator.Send(request);
        return result.ToApiResult();
    }

}
