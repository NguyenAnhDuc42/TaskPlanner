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
    public async Task<IActionResult> Create([FromBody] CreateFolderCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync<CreateFolderCommand, Guid>(command, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken ct)
    {
        var query = new GetFolderDetailQuery(id);
        var result = await _handler.QueryAsync<GetFolderDetailQuery, FolderDetailResponse>(query, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFolderRequest request, CancellationToken ct)
    {
        var command = new UpdateFolderCommand(
            FolderId: id,
            Name: request.Name,
            Color: request.Color,
            Icon: request.Icon,
            StartDate: request.StartDate,
            DueDate: request.DueDate,
            StatusId: request.StatusId,
            Priority: request.Priority
        );

        var result = await _handler.SendAsync(command, ct);
        return result.ToActionResult();
    }



    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _handler.SendAsync(new DeleteFolderCommand(id), ct);
        return result.ToActionResult();
    }
    
    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> GetTasks(Guid id, [FromBody] GetFolderTasksQuery request, CancellationToken ct = default)
    {
        var result = await _handler.QueryAsync<GetFolderTasksQuery, PagedResult<TaskRecord>>(request with { FolderId = id }, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}/tasks/batch")]
    public async Task<IActionResult> BatchUpdateTasks(Guid id, [FromBody] BatchUpdateFolderTasksCommand request, CancellationToken ct = default)
    {
        var result = await _handler.SendAsync(request with { FolderId = id }, ct);
        return result.ToActionResult();
    }
}


public record UpdateFolderRequest(
    string? Name,
    string? Color,
    string? Icon,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    Guid? StatusId,
    Priority? Priority
);




