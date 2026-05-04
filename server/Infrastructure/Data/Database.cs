using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using Domain.Common;

namespace Infrastructure.Data;

public class Database(TaskPlanDbContext context) : IDataBase
{
    private IDbContextTransaction? _currentTransaction;
    public IDbConnection Connection => context.Database.GetDbConnection();

    public DbSet<User> Users => context.Set<User>();
    public DbSet<UserPreference> UserPreferences => context.Set<UserPreference>();
    public DbSet<Session> Sessions => context.Set<Session>();
    public DbSet<ProjectWorkspace> Workspaces => context.Set<ProjectWorkspace>();
    public DbSet<ProjectSpace> Spaces => context.Set<ProjectSpace>();
    public DbSet<ProjectFolder> Folders => context.Set<ProjectFolder>();
    public DbSet<ProjectTask> Tasks => context.Set<ProjectTask>();
    public DbSet<Workflow> Workflows => context.Set<Workflow>();
    public DbSet<WorkspaceMember> WorkspaceMembers => context.Set<WorkspaceMember>();
    public DbSet<EntityAccess> Access => context.Set<EntityAccess>();
    public DbSet<Status> Statuses => context.Set<Status>();
    public DbSet<Comment> Comments => context.Set<Comment>();
    public DbSet<Document> Documents => context.Set<Document>();
    public DbSet<DocumentBlock> DocumentBlocks => context.Set<DocumentBlock>();
    public DbSet<ViewDefinition> ViewDefinitions => context.Set<ViewDefinition>();
    public DbSet<Attachment> Attachments => context.Set<Attachment>();
    public DbSet<EntityAssetLink> EntityAssetLinks => context.Set<EntityAssetLink>();
    public DbSet<TaskAssignment> TaskAssignments => context.Set<TaskAssignment>();
    public DbSet<PasswordResetToken> PasswordResetTokens => context.Set<PasswordResetToken>();

    public bool HasActiveTransaction => _currentTransaction != null;
    public ChangeTracker ChangeTracker => context.ChangeTracker;

    public IExecutionStrategy CreateExecutionStrategy() => context.Database.CreateExecutionStrategy();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return;
        _currentTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
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
        context.ChangeTracker.DetectChanges();
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        var strategy = CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
        });
    }
}
