using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Dapper;

namespace Application.Features.ViewFeatures;

public class GetViewsHandler(IDataBase db) : IQueryHandler<GetViewsQuery, List<ViewDto>>
{
    public async Task<Result<List<ViewDto>>> Handle(GetViewsQuery request, CancellationToken ct)
    {
        var views = await db.Connection.QueryAsync<ViewDto>(
            GetViewsSQL.GetViews,
            new { LayerId = request.LayerId, LayerType = request.LayerType.ToString() }
        );

        return Result<List<ViewDto>>.Success(views.ToList());
    }
}
