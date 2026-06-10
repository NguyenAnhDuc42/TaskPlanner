using Microsoft.EntityFrameworkCore;

namespace Application;

public class SetLayerWorkflowHandler(TaskPlanDbContext db, WorkspaceContext context) 
    : ICommandHandler<SetLayerWorkflowCommand>
{
    public async Task<Result> Handle(SetLayerWorkflowCommand request, CancellationToken cancellationToken)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        if (!request.WorkflowId.HasValue)
            return Result.Failure(Error.Validation("Workflow.Required", "Workflow ID is required."));

        var workflow = await db.Workflows.FirstOrDefaultAsync(w => w.Id == request.WorkflowId.Value, cancellationToken);
        if (workflow == null) return Result.Failure(Error.NotFound("Workflow.NotFound", "Workflow not found."));

        if (workflow.ProjectWorkspaceId != context.WorkspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        // Maintain "1 layer 1 workflow" by unassigning any existing workflow for this target
        if (request.FolderId.HasValue)
        {
            var existing = await db.Workflows.Where(w => w.ProjectFolderId == request.FolderId.Value).ToListAsync(cancellationToken);
            foreach (var w in existing) w.SetOwner(null, null);

            workflow.SetOwner(null, request.FolderId);
        }
        else if (request.SpaceId.HasValue)
        {
            var existing = await db.Workflows.Where(w => w.ProjectSpaceId == request.SpaceId.Value && w.ProjectFolderId == null).ToListAsync(cancellationToken);
            foreach (var w in existing) w.SetOwner(null, null);

            workflow.SetOwner(request.SpaceId, null);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


