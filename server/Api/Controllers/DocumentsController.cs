using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api;

[Route("api/documents")]
[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IHandler _handler;

    public DocumentsController(IHandler iHandler)
    {
        _handler = iHandler;
    }

    [HttpGet("{documentId:guid}/blocks")]
    public async Task<IActionResult> GetBlocks(Guid documentId, CancellationToken cancellationToken)
    {
        var query = new GetDocumentBlocksQuery(documentId);
        var result = await _handler.QueryAsync<GetDocumentBlocksQuery, List<DocumentBlockRecord>>(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{documentId:guid}/blocks")]
    public async Task<IActionResult> UpdateBlocks(Guid documentId, [FromBody] List<DocumentBlockValue> blocks, CancellationToken cancellationToken)
    {
        var command = new UpdateDocumentBlocksCommand(documentId, blocks);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

}


