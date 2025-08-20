using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;

namespace src.Feature.SpaceManager.GetSpaceInfo;

public class GetSpaceInfoHandler : IRequestHandler<GetSpaceInfoRequest, Result<List<TaskSummary>, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    public GetSpaceInfoHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<List<TaskSummary>, ErrorResponse>> Handle(GetSpaceInfoRequest request, CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                ""Id"",
                ""Name"",
                ""Priority"",
                ""StartDate"",
                ""DueDate""
            FROM ""Tasks""
            WHERE ""SpaceId"" = @SpaceId AND ""IsArchived"" = false
            ORDER BY ""CreatedAt"" ";

        var tasks = await _dbConnection.QueryAsync<TaskSummary>(query, new { SpaceId = request.spaceId });

        var taskList = new List<TaskSummary>(tasks.AsList());

        return Result<List<TaskSummary>, ErrorResponse>.Success(taskList);
    }
}
