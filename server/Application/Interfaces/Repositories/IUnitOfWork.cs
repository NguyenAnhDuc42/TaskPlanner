using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        ISessionRepository Sessions { get; }
    
        bool HasActiveTransaction { get; }
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        void DetachAllEntities();
        Task<int> ExecuteInTransactionAsync(Func<Task<int>> operation, CancellationToken cancellationToken = default);
    }
}
// public interface IUnitOfWork : IDisposable
// {
//     //transaction management
//     Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
//     Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
//     Task CommitTransactionAsync(CancellationToken cancellationToken = default);
//     Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

//     // Advanced features
//     Task<int> ExecuteInTransactionAsync(Func<Task<int>> operation, CancellationToken cancellationToken = default);
//     void DetachAllEntities();
//     bool HasActiveTransaction { get; }
// }