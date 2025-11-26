using Application.Contract.StatusContract;
using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.StatusManagement.GetStatusList;

public class GetStatusListHandler : IRequestHandler<GetStatusListQuery, List<StatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetStatusListHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<StatusDto>> Handle(GetStatusListQuery request, CancellationToken cancellationToken)
    {
        var statuses = await _unitOfWork.Set<Status>()
            .Where(s => s.LayerId == request.LayerId && s.LayerType == request.LayerType)
            .OrderBy(s => s.OrderKey)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return statuses.Adapt<List<StatusDto>>();
    }
}
