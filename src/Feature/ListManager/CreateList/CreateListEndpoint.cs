using Microsoft.AspNetCore.Mvc;
using src.Feature.ListManager.CreateList;
using src.Helper.Results;

namespace src.Feature.ListManager;

public partial class ListController
{
    [HttpPost]
    public async Task<IActionResult> CreateList(CreateListRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return result.ToApiResult();
    
    }
}