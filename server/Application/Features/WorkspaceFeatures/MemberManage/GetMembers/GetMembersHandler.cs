using System.Data;
using System.Text.Json;
using Application.Common.Filters;
using Application.Common.Results;
using Application.Contract.UserContract;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Dapper;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public class GetMembersHandler : IRequestHandler<GetMembersQuery, PagedResult<MemberDto>>
{
    private readonly IDbConnection _connection;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly WorkspaceContext _workspaceContext;
    private readonly CursorHelper _cursorHelper;
    private readonly HybridCache _cache;

    public GetMembersHandler(IDbConnection connection, IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper, HybridCache cache)
    {
        _connection = connection;
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _currentUserService = currentUserService;
        _workspaceContext = workspaceContext;
        _cursorHelper = cursorHelper;
        _cache = cache;
    }

    public async Task<PagedResult<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = _workspaceContext.workspaceId;
        var cacheKey = WorkspaceCacheKeys.MemberList(workspaceId, request);

        return await _cache.GetOrCreateAsync(cacheKey, async token =>
            {
                var pageSize = request.pagination.PageSize;
                DecodeCursor(request.pagination.Cursor, out var cursorTimestampm, out var cursorId);

                var sql = request.pagination.Direction == SortDirection.Ascending
                    ? GetMembersSQL.Asc
                    : GetMembersSQL.Desc;

                var rows = (await _connection.QueryAsync<MemberRow>(sql, new
                {
                    WorkspaceId = workspaceId,
                    name = request.filter.Name,
                    email = request.filter.Email,
                    role = request.filter.Role?.ToString(),
                    cursorTimestampm,
                    cursorId,
                    pageSizePLusOne = pageSize + 1
                }, commandType: CommandType.Text)).AsList();

                var hasMore = rows.Count > pageSize;
                if (hasMore) rows.RemoveAt(rows.Count - 1);

                var items = Map(rows);
                var nextCursor = hasMore && rows.Count > 0
                   ? EncodeNextCursor(rows[^1])
                   : null;

                return new PagedResult<MemberDto>(items, nextCursor, hasMore);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) });
    }

    private void DecodeCursor(string? cursor, out DateTimeOffset? ts, out Guid? id)
    {
        ts = null;
        id = null;
        if (string.IsNullOrEmpty(cursor)) return;

        var data = _cursorHelper.DecodeCursor(cursor);

        if (data?.Values == null) return;

        if (data.Values.TryGetValue("Timestamp", out var tsObj))
        {
            var tsStr = tsObj is JsonElement tsElement ? tsElement.GetString() : tsObj?.ToString();
            if (DateTimeOffset.TryParse(tsStr, out var parsedTs))
                ts = parsedTs;
        }
        if (data.Values.TryGetValue("Id", out var idObj))
        {
            var idStr = idObj is JsonElement idElement ? idElement.GetString() : idObj?.ToString();
            if (Guid.TryParse(idStr, out var parsedId))
                id = parsedId;
        }
    }


    private static List<MemberDto> Map(List<MemberRow> rows)
    {
        return rows.Select(x => new MemberDto
        {
            Id = x.Id,
            Name = x.Name,
            Email = x.Email,
            Role = x.Role
        }).ToList();
    }


    private string EncodeNextCursor(MemberRow last)
    {
        var cursorData = new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.UpdatedAt },
            { "Id", last.Id }
        });
        var encoded = _cursorHelper.EncodeCursor(cursorData);
        return encoded;
    }
}
