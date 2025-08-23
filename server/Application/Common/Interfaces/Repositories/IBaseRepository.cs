
using Domain.Common;

namespace Application.Common.Interfaces.Repositories;

public interface IBaseRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
}
