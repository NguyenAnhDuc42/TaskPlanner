
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;


namespace Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        DbSet<T> Set<T>() where T : class;
        bool HasActiveTransaction { get; }
        ChangeTracker ChangeTracker { get; }
        IExecutionStrategy CreateExecutionStrategy();
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default); 
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
    }
}