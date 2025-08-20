using System;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using src.Infrastructure.Abstractions.IRepositories;


namespace src.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly PlannerDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    private IUserRepository? _users;
    private ISessionRepository? _sessions;
    private IWorkspaceRepository? _workspaces;
    private ISpaceRepository? _spaces;
    private IPlanFolderRepository? _folders;
    private IPlanListRepository? _lists;
    private IPlanTaskRepository? _tasks;



    public UnitOfWork(PlannerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ISessionRepository Sessions => _sessions ??= new SessionRepository(_context);
    public IWorkspaceRepository Workspaces => _workspaces ??= new WorkspaceRepository(_context);
    public ISpaceRepository Spaces => _spaces ??= new SpaceRepository(_context);
    public IPlanFolderRepository Folders => _folders ??= new PlanFolderRepository(_context);
    public IPlanListRepository Lists => _lists ??= new PlanListRepository(_context);
    public IPlanTaskRepository Tasks => _tasks ??= new PlanTaskRepository(_context);
    
    public bool HasActiveTransaction => _currentTransaction != null;

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }
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
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }
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

    public void DetachAllEntities()
    {
        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Detached)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        // Resource disposal logic specific to synchronous disposal if any unmanaged resources existed.
        // All managed async disposables (DbContext, DbContextTransaction) are handled in DisposeAsyncCore.
        // For simplicity (as there are no explicit unmanaged resources here), this can remain empty or handle simple field cleanup.
        if (_disposed) return;
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync().ConfigureAwait(false);
                _currentTransaction = null;
            }
            if (_context != null)
            {
                await _context.DisposeAsync().ConfigureAwait(false);
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult(); // Blocks until async dispose is complete.
        Dispose(true); // Call the protected Dispose with true. This is the new standard pattern for sync-over-async cleanup.
        GC.SuppressFinalize(this);
    }

    public async Task<int> ExecuteInTransactionAsync(Func<Task<int>> operation, CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return await operation();
        }
        using var transaction = await BeginTransactionAsync(cancellationToken);
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


    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DBConcurrencyException("A concurrency conflict occurred while saving changes.", ex);
        }
        catch (DbUpdateException ex)
        {
            throw new Exception("An error occurred while saving changes to the database.", ex);
        }
    }
}
