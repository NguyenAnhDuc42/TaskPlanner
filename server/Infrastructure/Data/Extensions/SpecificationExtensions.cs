using Domain.Primitives;

namespace Infrastructure.Data.Extensions;

public static class SpecificationExtensions
{
    public static IQueryable<TEntity> WithSpecification<TEntity>(this IQueryable<TEntity> query, Specification<TEntity> specification)
        where TEntity : class
    {
        return query.Where(specification.Criteria);
    }
}
