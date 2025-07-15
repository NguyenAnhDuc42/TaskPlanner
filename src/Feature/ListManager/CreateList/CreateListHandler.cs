using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Feature.ListManager.CreateList;

public class CreateListHandler : IRequestHandler<CreateListRequest, Result<CreateListResponse, ErrorResponse>>
{
    private readonly PlannerDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateListHandler(PlannerDbContext context, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<CreateListResponse, ErrorResponse>> Handle(CreateListRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var list = PlanList.Create(request.name, request.workspaceId, request.spaceId, request.folderId, userId);
        await _context.Lists.AddAsync(list, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<CreateListResponse, ErrorResponse>.Success(new CreateListResponse(list.Id, "List created successfully"));
    }
}
