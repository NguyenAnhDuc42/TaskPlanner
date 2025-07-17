using System;
using System.Data;

using Dapper;
using MediatR;
using src.Helper.Results;

namespace src.Feature.ListManager.GetListInfo;

public class GetListInfoHandler : IRequestHandler<GetListInfoRequest, Result<TaskLineList, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    public GetListInfoHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<TaskLineList, ErrorResponse>> Handle(GetListInfoRequest request, CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                ""Id"",
                ""Name"",
                ""Priority"",
                ""Status"",
                ""StartDate"",
                ""DueDate""
            FROM ""Tasks""
            WHERE ""ListId"" = @ListId AND ""IsArchived"" = false
            ORDER BY ""CreatedAt""";

        var tasks = await _dbConnection.QueryAsync<TaskLineItem>(query, new { ListId = request.listId });
        var taskGroups = tasks.GroupBy(t => t.status).ToDictionary(g => g.Key, g => g.ToList());
        var taskList = new TaskLineList(taskGroups);
        return Result<TaskLineList, ErrorResponse>.Success(taskList);
    }
}