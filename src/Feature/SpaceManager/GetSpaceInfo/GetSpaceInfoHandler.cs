using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using src.Helper.Results;

namespace src.Feature.SpaceManager.GetSpaceInfo;

public class GetSpaceInfoHandler : IRequestHandler<GetSpaceInfoRequest, Result<TaskList, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    public GetSpaceInfoHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<TaskList, ErrorResponse>> Handle(GetSpaceInfoRequest request, CancellationToken cancellationToken)
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

        var tasks = await _dbConnection.QueryAsync<Task>(query, new { SpaceId = request.spaceId });

        var taskList = new TaskList(tasks.AsList());

        return Result<TaskList, ErrorResponse>.Success(taskList);
    }
}
