using Application.Common;
using Application.Common.Results;
using Application.Contract.UserContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public class GetMembersHandler : IRequestHandler<GetMembersQuery, PagedResult<MemberDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;

    public GetMembersHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper, HybridCache cache)
    {
        _cache = cache;
    }

    public Task<PagedResult<MemberDto>> Handle(GetMembersQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
