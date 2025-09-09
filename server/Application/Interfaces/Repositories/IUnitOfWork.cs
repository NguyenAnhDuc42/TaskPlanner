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
        IUserRepository Users { get; }
        ISessionRepository Sessions { get; }
        IProjectWorkspaceRepository ProjectWorkspaces { get; }
        IProjectSpaceRepository ProjectSpaces { get; }
        IProjectFolderRepository ProjectFolders { get; }
        IProjectListRepository ProjectLists { get; }
        IProjectTaskRepository ProjectTasks { get; }
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