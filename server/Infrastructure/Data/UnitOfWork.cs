using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Domain.Common.Interfaces;
using Infrastructure.Events.Extensions;
using System.Data;
using System.Data.Common;
using Dapper;
using Application.Interfaces;

namespace Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TaskPlanDbContext _context;
        private readonly IDomainEventDispatcher _domainDispatcher;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(TaskPlanDbContext context, IDomainEventDispatcher domainDispatcher)
        {
            _context = context;
            _domainDispatcher = domainDispatcher;
        }

        private IDbConnection Database => _context.Database.GetDbConnection();
        public DbSet<T> Set<T>() where T : class => _context.Set<T>();

        public bool HasActiveTransaction => _currentTransaction != null;

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null) return _currentTransaction;
            _currentTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,cancellationToken);
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

            // Collect and dispatch domain events
            var domainEvents = _context.ChangeTracker.CollectDomainEvents();
            while (domainEvents.Count > 0)
            {
                await _domainDispatcher.DispatchAsync(domainEvents, cancellationToken);
                _context.ChangeTracker.ClearDomainEvents();
                domainEvents = _context.ChangeTracker.CollectDomainEvents();
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

        #region Dapper Helpers

        private async Task EnsureConnectionOpenAsync(CancellationToken ct)
        {
            if (Database.State != ConnectionState.Open)
            {
                if (Database is DbConnection dbConn)
                    await dbConn.OpenAsync(ct);
                else
                    Database.Open();
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken ct = default)
        {
            await EnsureConnectionOpenAsync(ct);
            return await Database.QueryAsync<T>(sql, param, _currentTransaction?.GetDbTransaction());
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken ct = default)
        {
            await EnsureConnectionOpenAsync(ct);
            return await Database.QuerySingleOrDefaultAsync<T>(sql, param, _currentTransaction?.GetDbTransaction());
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken ct = default)
        {
            await EnsureConnectionOpenAsync(ct);
            return await Database.ExecuteAsync(sql, param, _currentTransaction?.GetDbTransaction());
        }

        #endregion
    }
}
