using Application.Helpers;
using Application.Common.Errors;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using server.Application.Interfaces;
using Application.Interfaces.Data;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public class DeleteTaskHandler : ICommandHandler<DeleteTaskCommand>
{
    private readonly IDataBase _db;

    public DeleteTaskHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        task.SoftDelete();
        await _db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
