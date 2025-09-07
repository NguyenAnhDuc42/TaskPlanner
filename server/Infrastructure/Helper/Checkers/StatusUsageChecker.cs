using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support;
using Infrastructure.Interfaces.Checkers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Helper.Checkers;

public class StatusUsageChecker : IStatusUsageChecker
{
    private readonly IUnitOfWork _unitOfWork;

    public StatusUsageChecker(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<int> GetUsageCountAsync(Guid statusId, CancellationToken ct = default)
    {
        return await _unitOfWork.Set<ProjectTask>().AsNoTracking().CountAsync(s => s.StatusId  == statusId, ct);
    }

    public async Task<IEnumerable<Guid>> GetUsageSampleIdsAsync(Guid statusId, int resultCount = 5, CancellationToken ct = default)
    {
        return await _unitOfWork.Set<ProjectTask>().AsNoTracking().Where(t => t.StatusId  == statusId).OrderBy(t => t.CreatedAt).Take(resultCount).Select(t => t.Id).ToListAsync(ct);
    }

    public async Task<bool> IsInUseAsync(Guid statusId, CancellationToken ct = default)
    {
        return await _unitOfWork.Set<ProjectTask>().AsNoTracking().AnyAsync(s => s.StatusId  == statusId, ct);
    }
}
