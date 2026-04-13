using Application.Features.FolderFeatures.SelfManagement.CreateFolder;
using Application.Features.FolderFeatures.SelfManagement.DeleteFolder;
using Application.Features.FolderFeatures.SelfManagement.UpdateFolder;
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFolderRequest request, CancellationToken ct)
    {
        var command = new UpdateFolderCommand(
            FolderId: id,
            Name: request.Name,
            Description: request.Description,
            Color: request.Color,
            Icon: request.Icon,
            IsPrivate: request.IsPrivate,
            StartDate: request.StartDate,
            DueDate: request.DueDate
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
}

public record UpdateFolderRequest(
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate
);
