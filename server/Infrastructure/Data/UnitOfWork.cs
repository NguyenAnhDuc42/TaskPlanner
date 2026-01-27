using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Infrastructure.Events.Extensions;
using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public ChangeTracker ChangeTracker => _context.ChangeTracker;

        public IExecutionStrategy CreateExecutionStrategy() => _context.Database.CreateExecutionStrategy();

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
                await SaveChangesAsync(cancellationToken);
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
            _context.ChangeTracker.DetectChanges();
            
            // Collect domain events before saving
            var domainEvents = _context.ChangeTracker.CollectDomainEvents();
            
            if (domainEvents.Any())
            {
                var outboxMessages = domainEvents.Select(domainEvent => new Domain.Entities.Support.OutboxMessage(
                    domainEvent.GetType().Name,
                    System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    DateTimeOffset.UtcNow)).ToList();

                _context.Set<Domain.Entities.Support.OutboxMessage>().AddRange(outboxMessages);
                
                // Clear the events so they aren't collected again in side-effect saves
                _context.ChangeTracker.ClearDomainEvents();
            }

            var totalChanges = await _context.SaveChangesAsync(cancellationToken);

            // NOTE: We still have the Mediator Dispatcher call for now to support 
            // immediate side-effects, but once we have the Worker fully running, 
            // we can transition most events to background-only.
            if (domainEvents.Any())
            {
                await _domainDispatcher.DispatchAsync(domainEvents, cancellationToken);
                
                // If the dispatcher triggered more events (side effects), save them too
                var sideEffectEvents = _context.ChangeTracker.CollectDomainEvents();
                if (sideEffectEvents.Any())
                {
                    totalChanges += await SaveChangesAsync(cancellationToken);
                }
            }

            return totalChanges;
        }

        public void DetachAllEntities()
        {
            var changedEntriesCopy = _context.ChangeTracker.Entries().ToList();
            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
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
