using Application.Interfaces.Repositories;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Infrastructure.Data.Extensions;
using Domain.Common.Interfaces;


namespace Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TaskPlanDbContext _context;
        private readonly IDomainEventDispatcher _domainDispatcher;

        private IDbContextTransaction? _currentTransaction;

        private IUserRepository? _users;
        private ISessionRepository? _sessions;
        private IProjectWorkspaceRepository? _projectWorkspaces;
        private IProjectSpaceRepository? _projectSpaces;
        private IProjectFolderRepository? _projectFolders;
        private IProjectListRepository? _projectLists;
        private IProjectTaskRepository? _projectTasks;

        public UnitOfWork(TaskPlanDbContext context, IDomainEventDispatcher domainDispatcher)
        {
            _context = context;
            _domainDispatcher = domainDispatcher;
        }
        #region Repositories
        public IUserRepository Users => _users ??= new UserRepository(_context);
        public ISessionRepository Sessions => _sessions ??= new SessionRepository(_context);
        public IProjectWorkspaceRepository ProjectWorkspaces => _projectWorkspaces ??= new ProjectWorkspaceRepository(_context);
        public IProjectSpaceRepository ProjectSpaces => _projectSpaces ??= new ProjectSpaceRepository(_context);
        public IProjectFolderRepository ProjectFolders => _projectFolders ??= new ProjectFolderRepository(_context);
        public IProjectListRepository ProjectLists => _projectLists ??= new ProjectListRepository(_context);
        public IProjectTaskRepository ProjectTasks => _projectTasks ??= new ProjectTaskRepository(_context);
        #endregion
        public DbSet<T> Set<T>() where T : class => _context.Set<T>();

        public bool HasActiveTransaction => _currentTransaction != null;

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null) return _currentTransaction;
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return _currentTransaction;
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null) return;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null) return;

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var snapshots = _context.ChangeTracker.CollectAggregateDomainEvents();
            var domainEvents = snapshots.FlattenEvents();

            if (domainEvents.Count > 0)
            {
                await _domainDispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
                snapshots.ClearDomainEventsFromSnapshot();
            }

            return result;
        }

        public void DetachAllEntities()
        {
            var changedEntriesCopy = _context.ChangeTracker.Entries().ToList();
            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }

        public async Task<int> ExecuteInTransactionAsync(Func<Task<int>> operation, CancellationToken cancellationToken = default)
        {
            if (HasActiveTransaction) return await operation();

            await using var transaction = await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await operation();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
