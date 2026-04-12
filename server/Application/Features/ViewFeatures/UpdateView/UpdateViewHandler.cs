using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;

namespace Application.Features.ViewFeatures.UpdateView;

public class UpdateViewHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateViewCommand>
{
    public async Task<Result> Handle(UpdateViewCommand request, CancellationToken ct)
    {
        var view = await db.Views.FindAsync([request.Id], ct);
        if (view == null) return ViewError.NotFound;

        if (context.CurrentMember.Role < Role.Admin && view.CreatorId != context.CurrentMember.Id)
            return MemberError.DontHavePermission;

        view.Name = request.Name;
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
