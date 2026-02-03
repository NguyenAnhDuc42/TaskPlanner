

using Application.Helper;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features;

public abstract class BaseQueryHandler : BaseFeatureHandler
{
    protected readonly CursorHelper CursorHelper;

    public BaseQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        CursorHelper = cursorHelper ?? throw new ArgumentNullException(nameof(cursorHelper));
    }

    /// <summary>
    /// Helper to get a read-only queryable for an entity.
    /// </summary>
    protected IQueryable<T> QueryNoTracking<T>() where T : class
    {
        return UnitOfWork.Set<T>().AsNoTracking();
    }
}
