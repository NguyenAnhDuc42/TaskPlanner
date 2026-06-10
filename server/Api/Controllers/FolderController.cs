using Microsoft.AspNetCore.Mvc;
namespace Api;

[Route("api/[controller]")]
[ApiController]    
public class FoldersController : ControllerBase
{
    private readonly IHandler _handler;

    public FoldersController(IHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFolderCommand command, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetFolderDetailQuery(id);
        var result = await _handler.QueryAsync<GetFolderDetailQuery, FolderDetailResponse>(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFolderCommand command, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(command with { FolderId = id }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(new DeleteFolderCommand(id), cancellationToken);
        return result.ToActionResult();
    }
    
    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> GetTasks(Guid id, [FromBody] GetFolderTasksQuery request, CancellationToken cancellationToken = default)
    {
        var result = await _handler.QueryAsync<GetFolderTasksQuery, PagedResult<TaskRecord>>(request with { FolderId = id }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}/tasks/batch")]
    public async Task<IActionResult> BatchUpdateTasks(Guid id, [FromBody] BatchUpdateFolderTasksCommand request, CancellationToken cancellationToken = default)
    {
        var result = await _handler.SendAsync(request with { FolderId = id }, cancellationToken);
        return result.ToActionResult();
    }
}







