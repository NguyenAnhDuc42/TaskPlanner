using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteTaskHandler(TaskPlanDbContext db) : ICommandHandler<DeleteTaskCommand>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var task = await db.ProjectTasks
            .ById(request.TaskId)
            .FirstOrDefaultAsync(ct);

        if (task == null) 
            return Result.Failure(TaskError.NotFound);

        task.SoftDelete();
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}


