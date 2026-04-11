using Application.Common.Errors;
using Application.Common.Results;
using Application.Features.ViewFeatures.FeatureHelpers;
using Application.Interfaces.Data;
using server.Application.Interfaces;

namespace Application.Features.ViewFeatures.GetViewData;

public class GetViewDataHandler : IQueryHandler<GetViewDataQuery, BaseViewResult>
{
    private readonly IDataBase _db;
    private readonly ViewBuilder _viewBuilder;

    public GetViewDataHandler(IDataBase db, ViewBuilder viewBuilder)
    {
        _db = db;
        _viewBuilder = viewBuilder;
    }

    public async Task<Result<BaseViewResult>> Handle(GetViewDataQuery request, CancellationToken ct)
    {
        var view = await _db.Views.FindAsync(request.ViewId, ct);
        if (view == null) return ViewError.NotFound;

        return await _viewBuilder.Build(view.LayerId, view.LayerType, view);
    }
}
