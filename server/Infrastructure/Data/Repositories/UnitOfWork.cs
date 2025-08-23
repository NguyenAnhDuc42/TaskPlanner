using System;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Application.Common.Interfaces.Repositories; // Assuming these interfaces are in this namespace

namespace Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly TaskPlanDbContext _context;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    private IProjectFolderRepository? _projectFolders;
    private IProjectListRepository? _projectLists;
    private IProjectSpaceRepository? _projectSpaces;
    private IProjectTaskRepository? _projectTasks;
    private IProjectWorkspaceRepository? _projectWorkspaces;
    private IAttachmentRepository? _attachments;
    private IChecklistRepository? _checklists;
    private ICommentRepository? _comments;
    private INotificationRepository? _notifications;
    private ISessionRepository? _sessions;
    private IStatusRepository? _statuses;
    private ITimeLogRepository? _timeLogs;
    private IUserRepository? _users;

    public UnitOfWork(TaskPlanDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IProjectFolderRepository ProjectFolders => _projectFolders ??= new ProjectFolderRepository(_context);
    public IProjectListRepository ProjectLists => _projectLists ??= new ProjectListRepository(_context);
    public IProjectSpaceRepository ProjectSpaces => _projectSpaces ??= new ProjectSpaceRepository(_context);
    public IProjectTaskRepository ProjectTasks => _projectTasks ??= new ProjectTaskRepository(_context);
    public IProjectWorkspaceRepository ProjectWorkspaces => _projectWorkspaces ??= new ProjectWorkspaceRepository(_context);
    public IAttachmentRepository Attachments => _attachments ??= new AttachmentRepository(_context);
    public IChecklistRepository Checklists => _checklists ??= new ChecklistRepository(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public ISessionRepository Sessions => _sessions ??= new SessionRepository(_context);
    public IStatusRepository Statuses => _statuses ??= new StatusRepository(_context);
    public ITimeLogRepository TimeLogs => _timeLogs ??= new TimeLogRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);

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
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        Dispose(true);
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