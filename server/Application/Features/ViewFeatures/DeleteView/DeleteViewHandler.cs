using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;

namespace Application.Features.ViewFeatures.DeleteView;

public class DeleteViewHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<DeleteViewCommand>
{
    public async Task<Result> Handle(DeleteViewCommand request, CancellationToken ct)
    {
        var view = await db.Views.FindAsync([request.Id], ct);
        if (view == null) return ViewError.NotFound;

        if (context.CurrentMember.Role < Role.Admin && view.CreatorId != context.CurrentMember.Id)
            return MemberError.DontHavePermission;

        db.Views.Remove(view);
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
