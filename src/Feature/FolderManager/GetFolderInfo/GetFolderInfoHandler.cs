using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using src.Feature.SpaceManager.GetSpaceInfo;
using src.Helper.Results;

namespace src.Feature.FolderManager.GetFolderInfo;

public class GetFolderInfoHandler : IRequestHandler<GetFolderInfoRequest, Result<TaskList, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    public GetFolderInfoHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<TaskList, ErrorResponse>> Handle(GetFolderInfoRequest request, CancellationToken cancellationToken)
    {
        const string query = @"
            SELECT
                ""Id"",
                ""Name"",
                ""Priority"",
                ""StartDate"",
                ""DueDate""
            FROM ""Tasks""
            WHERE ""FolderId"" = @FolderId AND ""IsArchived"" = false
            ORDER BY ""CreatedAt""";

        var tasks = await _dbConnection.QueryAsync<Task>(query, new { FolderId = request.folderId });
        var taskList = new TaskList(tasks.AsList());
        return Result<TaskList, ErrorResponse>.Success(taskList);
    }
}