using Application.Features.FolderFeatures;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

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
        var result = await _handler.QueryAsync<GetFolderDetailQuery, FolderDetailDto>(query, ct);
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
            IsPrivate: request.IsPrivate,
            StartDate: request.StartDate,
            DueDate: request.DueDate,
            StatusId: request.StatusId
        );

        var result = await _handler.SendAsync(command, ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/move-status")]
    public async Task<IActionResult> MoveStatus(Guid id, [FromBody] MoveFolderToStatusRequest request, CancellationToken ct)
    {
        var command = new MoveFolderToStatusCommand(
            FolderId: id,
            TargetStatusId: request.TargetStatusId,
            PreviousItemOrderKey: request.PreviousItemOrderKey,
            NextItemOrderKey: request.NextItemOrderKey,
            NewOrderKey: request.NewOrderKey
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

    [HttpGet("{id:guid}/items")]
    public async Task<IActionResult> GetItems(Guid id, CancellationToken ct)
    {
        var result = await _handler.QueryAsync<GetFolderItemsQuery, Application.Features.ViewFeatures.TaskViewData>(new GetFolderItemsQuery(id), ct);
        return result.ToActionResult();
    }
}

public record UpdateFolderRequest(
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    Guid? StatusId
);

public record MoveFolderToStatusRequest(
    Guid? TargetStatusId,
    string? PreviousItemOrderKey,
    string? NextItemOrderKey,
    string? NewOrderKey = null
);
