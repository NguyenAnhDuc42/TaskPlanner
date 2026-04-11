using Application.Interfaces.Data;
using Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures.GetViews;

public class GetViewsHandler : IQueryHandler<GetViewsQuery, List<ViewDto>>
{
    private readonly IDataBase _db;

    public GetViewsHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result<List<ViewDto>>> Handle(GetViewsQuery request, CancellationToken ct)
    {
        var views = await _db.Views
            .AsNoTracking()
            .Where(v => v.LayerId == request.LayerId && v.LayerType == request.LayerType)
            .OrderBy(v => v.CreatedAt)
            .Select(v => new ViewDto(
                v.Id,
                v.Name,
                v.ViewType,
                v.IsDefault
            ))
            .ToListAsync(ct);

        return views;
    }
}
