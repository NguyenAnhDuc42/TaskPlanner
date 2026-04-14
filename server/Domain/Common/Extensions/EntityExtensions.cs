using System.Linq;
using Domain.Common;

namespace Domain.Entities;

public static class EntityExtensions
{
    public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query) where T : Entity
    {
        return query.Where(x => x.DeletedAt == null);
    }
}
