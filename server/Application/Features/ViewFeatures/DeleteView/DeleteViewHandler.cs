using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities.ProjectEntities;

namespace Application.Features.ViewFeatures.DeleteView;

public class DeleteViewHandler : ICommandHandler<DeleteViewCommand>
{
    private readonly IDataBase _db;

    public DeleteViewHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteViewCommand request, CancellationToken ct)
    {
        var view = await _db.Views.FindAsync(request.Id, ct);
        if (view == null) return ViewError.NotFound;

        _db.Views.Remove(view);
        await _db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
