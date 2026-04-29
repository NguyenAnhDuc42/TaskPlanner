using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ViewFeatures;

public partial class GetViewDataHandler(IDataBase db) 
    : IQueryHandler<GetViewDataQuery, ViewDataResponse>
{
    public async Task<Result<ViewDataResponse>> Handle(GetViewDataQuery request, CancellationToken ct)
    {
        var view = await db.ViewDefinitions.FirstOrDefaultAsync(v => v.Id == request.ViewId, ct);
        if (view == null) return Result<ViewDataResponse>.Failure(Error.NotFound("View.NotFound", "View definition not found."));

        var workflow = await WorkflowHelper.GetActiveWorkflow(db, view.ProjectWorkspaceId, 
            view.ProjectSpaceId, 
            view.ProjectFolderId, ct);

        object data;
        switch (view.ViewType)
        {
            case ViewType.Tasks:
                data = await FetchTaskBoardData(db, view, workflow, ct);
                break;
            case ViewType.Overview:
                data = await FetchOverviewContextData(db, view, workflow, ct);
                break;
            default:
                return Result<ViewDataResponse>.Failure(Error.Validation("View.InvalidType", "Unsupported view type."));
        }

        return Result<ViewDataResponse>.Success(new ViewDataResponse(view.Id, view.ViewType, data));
    }
}
