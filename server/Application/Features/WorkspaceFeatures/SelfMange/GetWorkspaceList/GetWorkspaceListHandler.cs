using System.Linq;
using Application.Common.Filters;
using Application.Common.Results;
using Application.Contract.UserContract;
using Application.Contract.WorkspaceContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfMange.GetWorkspaceList;

public class GetWorkspaceListHandler : IRequestHandler<GetWorksapceListQuery, PagedResult<WorkspaceDetail>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly CursorHelper _cursorHelper;

    public GetWorkspaceListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, CursorHelper cursorHelper)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cursorHelper = cursorHelper;
    }
    public async Task<PagedResult<WorkspaceDetail>> Handle(GetWorksapceListQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        var pageSize = request.Pagination.PageSize;

        var items = await _unitOfWork.Set<ProjectWorkspace>()
            .ApplyFilter(request.filter, currentUserId)
            .ApplyCursor(request.Pagination, _cursorHelper)
            .ApplySort(request.Pagination)
            .Take(pageSize + 1) // +1 to check if more exists
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        var displayItems = items.Take(pageSize).ToList();


        var workspaceIds = displayItems.Select(w => w.Id).ToList();
        var membersByWorkspace = await GetMembersByWorkspaces(workspaceIds);

        var dtos = displayItems.Select(w =>
        {
            var dto = w.Adapt<WorkspaceDetail>();
            var members = membersByWorkspace.ContainsKey(w.Id) ? membersByWorkspace[w.Id] : new List<Member>();
            return dto with { Members = members };
        }).ToList();

        string? nextCursor = null;
        if (hasMore && displayItems.Count > 0)
        {
            var lastItem = displayItems.Last();
            nextCursor = _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Id", lastItem.Id },
                { "Timestamp", lastItem.UpdatedAt }
            }));
        }

        return new PagedResult<WorkspaceDetail>(dtos, nextCursor, hasMore);
    }

    private async Task<Dictionary<Guid, List<Member>>> GetMembersByWorkspaces(IEnumerable<Guid> workspaceIds)
    {
        string sql = @"
        SELECT wm.project_workspace_id, u.name, u.email, wm.role
        FROM workspace_members wm 
        JOIN user u ON wm.user_id = u.id
        WHERE wm.project_workspace_id IN @WorkspaceIds
        ";

        var membersByWorkspace = await _unitOfWork.QueryAsync<dynamic>(sql, new { WorkspaceIds = workspaceIds });
        var memberDict = membersByWorkspace
            .GroupBy(m => (Guid)m.ProjectWorkspaceId)
            .ToDictionary(g => g.Key, g => g.Select(m => new Member
            {
                Id = m.Id,
                Username = m.Username,
                Email = m.Email,
                Role = (Role)m.Role
            }).ToList());

        return memberDict;
    }

    // EF Core version if have in future decide to add in navprops
    // private async Task<Dictionary<Guid, List<Member>>> GetMembersByWorkspaces(List<Guid> workspaceIds, CancellationToken cancellationToken)
    // {
    //     var members = await _unitOfWork.Set<WorkspaceMember>()
    //         .Include(wm => wm.User)
    //         .Where(wm => workspaceIds.Contains(wm.ProjectWorkspaceId))
    //         .Select(wm => new Member
    //         {
    //             Id = wm.UserId,
    //             Username = wm.User.Username,
    //             Email = wm.User.Email,
    //             Role = wm.Role
    //         })
    //         .AsNoTracking()
    //         .ToListAsync(cancellationToken);

    //     return members
    //         .GroupBy(m => m.ProjectWorkspaceId)
    //         .ToDictionary(
    //             g => g.Key,
    //             g => g.Select(m => new Member
    //             {
    //                 Id = m.UserId,
    //                 Username = m.Username,
    //                 Email = m.Email,
    //                 Role = m.Role
    //             }).ToList()
    //         );
    // }

}


