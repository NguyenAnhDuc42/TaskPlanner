using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkflowFeatures.SetLayerWorkflow;

public class SetLayerWorkflowHandler(IDataBase db, WorkspaceContext context) 
    : ICommandHandler<SetLayerWorkflowCommand>
{
    public async Task<Result> Handle(SetLayerWorkflowCommand request, CancellationToken ct)
    {
        // 1. Permission Check (Only Admin or above can manage workflows)
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        // 2. Validate Target and Update
        if (request.FolderId.HasValue)
        {
            var folder = await db.Folders.FirstOrDefaultAsync(f => f.Id == request.FolderId, ct);
            if (folder == null) return Result.Failure(FolderError.NotFound);

            folder.UpdateWorkflow(request.WorkflowId);
        }
        else if (request.SpaceId.HasValue)
        {
            var space = await db.Spaces.FirstOrDefaultAsync(s => s.Id == request.SpaceId, ct);
            if (space == null) return Result.Failure(SpaceError.NotFound);

            space.UpdateWorkflow(request.WorkflowId);
        }
        else
        {
            return Result.Failure(Error.Validation("Workflow.NoTarget", "At least one target (Space or Folder) must be specified."));
        }

        // 3. Persist Changes
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
