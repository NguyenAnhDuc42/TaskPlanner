using Application.Common.Results;
using Application.Contract.UserContract;
using Application.Contract.WorkspaceContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System.Runtime.CompilerServices;

namespace Application.Features.WorkspaceFeatures.GetWorkspaceList;

public class GetWorkspaceListHandler : IRequestHandler<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>>
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
    public async Task<PagedResult<WorkspaceSummaryDto>> Handle(GetWorksapceListQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        var pageSize = request.Pagination.PageSize;

        var query = _unitOfWork.Set<ProjectWorkspace>()
            .Where(w => w.Members.Any(m => m.UserId == currentUserId));
        var baseQuery = query
            .ApplyFilter(request.filter, currentUserId)
            .ApplyCursor(request.Pagination, _cursorHelper)
            .ApplySort(request.Pagination);

        var rawItems = await baseQuery
            .Take(pageSize + 1) // fetch one extra to determine hasMore
            .Select(w => new
            {
                Workspace = w,
                Role = w.Members.Where(m => m.UserId == currentUserId).Select(m => m.Role).Single(),
                MemberCount = w.Members.Count()
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var hasMore = rawItems.Count > pageSize;
        if (hasMore) rawItems.RemoveAt(rawItems.Count - 1); // drop the extra

        // Map to DTOs (no UpdatedAt on DTO)
        var dtos = rawItems.Select(x => new WorkspaceSummaryDto
        {
            Id = x.Workspace.Id,
            Name = x.Workspace.Name,
            Icon = x.Workspace.Customization.Icon,
            Color = x.Workspace.Customization.Color,
            Variant = x.Workspace.Variant,
            Role = x.Role,
            MemberCount = x.MemberCount
        }).ToList();

        // Build next cursor using the last workspace's UpdatedAt + Id (matches your contract)
        string? nextCursor = null;
        if (hasMore && rawItems.Count > 0)
        {
            var lastWorkspace = rawItems.Last().Workspace;
            nextCursor = _cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Id", lastWorkspace.Id },
                { "Timestamp", lastWorkspace.UpdatedAt }
            }));
        }

        return new PagedResult<WorkspaceSummaryDto>(dtos, nextCursor, hasMore);
    }




}


