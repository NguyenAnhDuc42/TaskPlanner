using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures;

public class UpdateViewHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateViewCommand>
{
    public async Task<Result> Handle(UpdateViewCommand request, CancellationToken ct)
    {
        var view = await db.ViewDefinitions.FirstOrDefaultAsync(v => v.Id == request.Id, ct);
        if (view == null) 
            return Result.Failure(ViewError.NotFound);

        if (context.CurrentMember.Role > Role.Admin && view.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        if (!string.IsNullOrEmpty(request.Name))
        {
            view.UpdateName(request.Name);
        }
        
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
