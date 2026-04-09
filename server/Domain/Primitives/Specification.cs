using System.Linq.Expressions;

namespace Domain.Primitives;

public class Specification<TEntity>(Expression<Func<TEntity, bool>> criteria)
{
    public Expression<Func<TEntity, bool>> Criteria { get; } = criteria;

    public bool IsSatisfiedBy(TEntity entity)
    {
        return Criteria.Compile()(entity);
    }

    public static implicit operator Expression<Func<TEntity, bool>>(Specification<TEntity> specification)
    {
        return specification.Criteria;
    }
}
