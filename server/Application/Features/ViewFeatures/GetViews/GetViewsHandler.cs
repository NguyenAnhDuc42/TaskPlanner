using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures.GetViews;

public class GetViewsHandler(IDataBase db) : IQueryHandler<GetViewsQuery, List<ViewDto>>
{
    public async Task<Result<List<ViewDto>>> Handle(GetViewsQuery request, CancellationToken ct)
    {
        var views = await db.ViewDefinitions
            .AsNoTracking()
            .ByLayer(request.LayerId, request.LayerType)
            .OrderBy(v => v.SortOrder)
            .ThenBy(v => v.CreatedAt)
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
