using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Contract.WorkspaceContract;
using Domain.Enums.Workspace;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Application.Features.WorkspaceFeatures.SelfMange.GetWorkspaceList;

public record class GetWorksapceListQuery(CursorPaginationRequest Pagination, WorkspaceFilter filter) : IQuery<PagedResult<WorkspaceDetail>>;
public record WorkspaceFilter(string? Name = null, bool Owned = false, bool isArchived = false, WorkspaceVariant? Variant = null);