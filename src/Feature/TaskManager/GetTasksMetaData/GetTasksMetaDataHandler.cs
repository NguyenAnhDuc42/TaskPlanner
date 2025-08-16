using System;
using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.TaskManager.GetTasksMetaData;

public class GetTasksMetaDataHandler : IRequestHandler<GetTasksMetaDataRequest, PagedResult<TasksSummary>>
{
    public Task<PagedResult<TasksSummary>> Handle(GetTasksMetaDataRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
