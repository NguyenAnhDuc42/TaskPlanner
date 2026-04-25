using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using Infrastructure.Events.Extensions;
using Dapper;

namespace Infrastructure.Data;

public class Database(TaskPlanDbContext context) : IDataBase
{
    private IDbContextTransaction? _currentTransaction;

    public IDbConnection Connection => context.Database.GetDbConnection();

    public DbSet<User> Users => context.Set<User>();
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
    public DbSet<Dashboard> Dashboards => context.Set<Dashboard>();
    public DbSet<ViewDefinition> ViewDefinitions => context.Set<ViewDefinition>();
    public DbSet<Attachment> Attachments => context.Set<Attachment>();
    public DbSet<EntityAssetLink> EntityAssetLinks => context.Set<EntityAssetLink>();
    public DbSet<TaskAssignment> TaskAssignments => context.Set<TaskAssignment>();
    public DbSet<Widget> Widgets => context.Set<Widget>();
    public DbSet<OutboxMessage> OutboxMessages => context.Set<OutboxMessage>();
    public DbSet<PasswordResetToken> PasswordResetTokens => context.Set<PasswordResetToken>();

    public DbSet<T> Set<T>() where T : class => context.Set<T>();
    public bool HasActiveTransaction => _currentTransaction != null;
    public ChangeTracker ChangeTracker => context.ChangeTracker;

    public IExecutionStrategy CreateExecutionStrategy() => context.Database.CreateExecutionStrategy();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return _currentTransaction;
        _currentTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
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
        context.ChangeTracker.DetectChanges();
        var domainEvents = context.ChangeTracker.CollectDomainEvents();
        var hasOutboxWork = domainEvents.Any();
        
        if (hasOutboxWork)
        {
            var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage(
                domainEvent.GetType().Name,
                System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                DateTimeOffset.UtcNow)).ToList();

            context.Set<OutboxMessage>().AddRange(outboxMessages);
            context.ChangeTracker.ClearDomainEvents();
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

}
