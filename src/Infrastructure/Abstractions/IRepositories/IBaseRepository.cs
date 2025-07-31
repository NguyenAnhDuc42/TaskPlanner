using System;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IBaseRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    void Add(TEntity entity);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

}
