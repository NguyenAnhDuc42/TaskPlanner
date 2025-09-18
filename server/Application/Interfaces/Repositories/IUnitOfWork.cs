using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        DbSet<T> Set<T>() where T : class;
        bool HasActiveTransaction { get; }
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        void DetachAllEntities();
        Task<int> ExecuteInTransactionAsync(Func<Task<int>> operation, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
    }
}