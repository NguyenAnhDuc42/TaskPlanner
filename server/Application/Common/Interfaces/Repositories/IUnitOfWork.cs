using System;
using Microsoft.EntityFrameworkCore.Storage;


namespace Application.Common.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    //transaction management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    // Advanced features
    Task<int> ExecuteInTransactionAsync(Func<Task<int>> operation, CancellationToken cancellationToken = default);
    void DetachAllEntities();
    bool HasActiveTransaction { get; }
}
