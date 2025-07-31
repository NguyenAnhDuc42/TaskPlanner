using System;
using Microsoft.EntityFrameworkCore;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly PlannerDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public BaseRepository(PlannerDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(id, cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(id, cancellationToken) != null;
    }

    public virtual void Add(TEntity entity)
    {
        DbSet.Add(entity);
    }
}
