using Application.Common.Errors;
using Application.Interfaces.Data;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public class DeleteSpaceHandler : ICommandHandler<DeleteSpaceCommand>
{
    private readonly IDataBase _db;

    public DeleteSpaceHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken ct)
    {
        var space = await _db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) return Result.Failure(SpaceError.NotFound);

        space.SoftDelete();
        
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
