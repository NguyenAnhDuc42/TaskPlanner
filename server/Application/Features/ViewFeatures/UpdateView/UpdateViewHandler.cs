using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities.ProjectEntities;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.UpdateView;

public class UpdateViewHandler : ICommandHandler<UpdateViewCommand>
{
    private readonly IDataBase _db;

    public UpdateViewHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateViewCommand request, CancellationToken ct)
    {
        var view = await _db.Views.FindAsync(request.Id, ct);
        if (view == null) return ViewError.NotFound;

        view.Update(request.Name, request.IsDefault);
        if (request.FilterConfigJson != null || request.DisplayConfigJson != null)
        {
            view.UpdateConfigs(
                request.FilterConfigJson ?? view.FilterConfigJson,
                request.DisplayConfigJson ?? view.DisplayConfigJson
            );
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
