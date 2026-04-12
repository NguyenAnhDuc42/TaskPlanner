using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures.GetViews;

public class GetViewsHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetViewsQuery, List<ViewDto>>
{
    public async Task<Result<List<ViewDto>>> Handle(GetViewsQuery request, CancellationToken ct)
    {
        var views = await db.Views
            .AsNoTracking()
            .ByLayer(request.LayerId, request.LayerType)
            .Select(v => new ViewDto(
                v.Id,
                v.Name,
                v.ViewType,
                v.IsDefault
            ))
            .ToListAsync(ct);

        return Result<List<ViewDto>>.Success(views);
    }
}
