using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using src.Feature.SpaceManager.GetSpaceInfo;
using src.Helper.Results;

namespace src.Feature.ListManager.GetListInfo;

public class GetListInfoHandler : IRequestHandler<GetListInfoRequest, Result<TaskList, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    public GetListInfoHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<TaskList, ErrorResponse>> Handle(GetListInfoRequest request, CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                ""Id"",
                ""Name"",
                ""Priority"",
                ""StartDate"",
                ""DueDate""
            FROM ""Tasks""
            WHERE ""ListId"" = @ListId AND ""IsArchived"" = false
            ORDER BY ""CreatedAt""";

        var tasks = await _dbConnection.QueryAsync<Task>(query, new { ListId = request.listId });
        var taskList = new TaskList(tasks.AsList());
        return Result<TaskList, ErrorResponse>.Success(taskList);
    }
}