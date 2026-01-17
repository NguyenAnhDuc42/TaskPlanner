using Application.Common.Filters;
using Application.Common.Results;
using Application.Contract.WorkspaceContract;
using Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;
using Application.Helper;
using Application.Interfaces.Repositories;
using Dapper;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System.Data;
using System.Text.Json;


namespace Application.Features.WorkspaceFeatures.GetWorkspaceList;

public class GetWorkspaceListHandler : IRequestHandler<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>>
{
    public readonly IDbConnection _connection;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly CursorHelper _cursorHelper;


    public GetWorkspaceListHandler(IDbConnection dbConnection,IUnitOfWork unitOfWork, ICurrentUserService currentUserService, CursorHelper cursorHelper)
    {
        _connection = dbConnection;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cursorHelper = cursorHelper;
    }

    public async Task<PagedResult<WorkspaceSummaryDto>> Handle(GetWorksapceListQuery request,CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        var pageSize = request.Pagination.PageSize;


        DecodeCursor(request.Pagination.Cursor, out var cursorTs, out var cursorId);


        var sql = request.Pagination.Direction == SortDirection.Ascending
        ? GetWorkspaceListSQL.Asc
        : GetWorkspaceListSQL.Desc;

        var variantString = request.filter.Variant?.ToString();

        var rows = (await _connection.QueryAsync<WorkspaceRow>(sql, new
        {
            CurrentUserId = currentUserId,
            name = request.filter.Name,
            owned = request.filter.Owned,
            isArchived = request.filter.isArchived,

            variant = variantString,   // important

            cursorTimestamp = cursorTs,
            cursorId = cursorId,

            PageSizePLusOne = pageSize + 1
        })).AsList();


        var hasMore = rows.Count > pageSize;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);


        var items = Map(rows);


        var nextCursor = hasMore && rows.Count > 0
        ? EncodeNextCursor(rows[^1])
        : null;


        return new PagedResult<WorkspaceSummaryDto>(items, nextCursor, hasMore);
    }
   
    private void DecodeCursor(string? cursor, out DateTimeOffset? ts, out Guid? id)
    {
        ts = null;
        id = null;

                 Console.WriteLine($"DEBUG: Raw cursor = {cursor}");
        if (string.IsNullOrEmpty(cursor)) return;

        var data = _cursorHelper.DecodeCursor(cursor);
        Console.WriteLine($"DEBUG: Decoded data = {JsonSerializer.Serialize(data)}"); 

        if (data?.Values == null)  return;


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
        Console.WriteLine($"DEBUG: Decoded ts = {ts}, id = {id}"); // ← Add
    }


    private static List<WorkspaceSummaryDto> Map(List<WorkspaceRow> rows)
    {
        return rows.Select(x => new WorkspaceSummaryDto
        {
            Id = x.Id,
            Name = x.Name,
            Icon = x.Icon,
            Color = x.Color,
            Description = x.Description,
            Variant = x.Variant,
            Role = x.Role,
            MemberCount = x.MemberCount
        }).ToList();
    }


    private string EncodeNextCursor(WorkspaceRow last)
    {
        Console.WriteLine($"DEBUG ENCODE: Timestamp = {last.UpdatedAt}, Id = {last.Id}");
        
        var cursorData = new CursorData(new Dictionary<string, object>
        {
            { "Timestamp", last.UpdatedAt },
            { "Id", last.Id }
        });
        
        Console.WriteLine($"DEBUG ENCODE: Before encryption = {JsonSerializer.Serialize(cursorData.Values)}");
        
        var encoded = _cursorHelper.EncodeCursor(cursorData);
        
        Console.WriteLine($"DEBUG ENCODE: After encryption = {encoded}");
        
        return encoded;
    }

    //public async Task<PagedResult<WorkspaceSummaryDto>> Handle(GetWorksapceListQuery request, CancellationToken cancellationToken)
    //{
    //    var currentUserId = _currentUserService.CurrentUserId();
    //    var pageSize = request.Pagination.PageSize;

    //    var query = _unitOfWork.Set<ProjectWorkspace>()
    //        .Where(w => w.Members.Any(m => m.UserId == currentUserId));

    //    var baseQuery = query
    //        .ApplyFilter(request.filter, currentUserId)
    //        .ApplyCursor(request.Pagination, _cursorHelper)
    //        .ApplySort(request.Pagination);

    //    var rawItems = await baseQuery
    //        .Take(pageSize + 1) // fetch one extra to determine hasMore
    //        .Select(w => new    
    //        {
    //            Workspace = w,
    //            Role = w.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role).Single(),
    //            MemberCount = w.Members.Count()
    //        })
    //        .AsNoTracking()
    //        .ToListAsync(cancellationToken);

    //    var hasMore = rawItems.Count > pageSize;
    //    if (hasMore) rawItems.RemoveAt(rawItems.Count - 1); // drop the extra

    //    // Map to DTOs (no UpdatedAt on DTO)
    //    var dtos = rawItems.Select(x => new WorkspaceSummaryDto
    //    {
    //        Id = x.Workspace.Id,
    //        Name = x.Workspace.Name,
    //        Icon = x.Workspace.Customization.Icon,
    //        Color = x.Workspace.Customization.Color,
    //        Description = x.Workspace.Description,
    //        Variant = x.Workspace.Variant,
    //        Role = x.Role,
    //        MemberCount = x.MemberCount
    //    }).ToList();

    //    // Build next cursor using the last workspace's UpdatedAt + Id (matches your contract)
    //    string? nextCursor = null;
    //    if (hasMore && rawItems.Count > 0)
    //    {
    //        var lastWorkspace = rawItems.Last().Workspace;
    //        nextCursor = _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
    //        {
    //            { "Id", lastWorkspace.Id },
    //            { "Timestamp", lastWorkspace.UpdatedAt }
    //        }));
    //    }

    //    return new PagedResult<WorkspaceSummaryDto>(dtos, nextCursor, hasMore);
    //}

}


