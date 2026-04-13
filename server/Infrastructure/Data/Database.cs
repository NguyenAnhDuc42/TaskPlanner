using Application.Interfaces.Data;
using Application.Interfaces.Repositories;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using Infrastructure.Events.Extensions;
using Hangfire;
using Dapper;

namespace Infrastructure.Data;

public class Database : IDataBase
{
    private readonly TaskPlanDbContext _context;
    private readonly IDomainEventDispatcher _domainDispatcher;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private IDbContextTransaction? _currentTransaction;

    public Database(
        TaskPlanDbContext context, 
        IDomainEventDispatcher domainDispatcher,
        IBackgroundJobClient backgroundJobClient)
    {
        _context = context;
        _domainDispatcher = domainDispatcher;
        _backgroundJobClient = backgroundJobClient;
    }

    public IDbConnection Connection => _context.Database.GetDbConnection();

    public DbSet<User> Users => _context.Set<User>();
    public DbSet<Session> Sessions => _context.Set<Session>();
    public DbSet<ProjectWorkspace> Workspaces => _context.Set<ProjectWorkspace>();
    public DbSet<ProjectSpace> Spaces => _context.Set<ProjectSpace>();
    public DbSet<ProjectFolder> Folders => _context.Set<ProjectFolder>();
    public DbSet<ProjectTask> Tasks => _context.Set<ProjectTask>();
    public DbSet<Workflow> Workflows => _context.Set<Workflow>();
    public DbSet<WorkspaceMember> WorkspaceMembers => _context.Set<WorkspaceMember>();
    public DbSet<EntityAccess> Access => _context.Set<EntityAccess>();
    public DbSet<Status> Statuses => _context.Set<Status>();
    public DbSet<Comment> Comments => _context.Set<Comment>();
    public DbSet<Document> Documents => _context.Set<Document>();
    public DbSet<Dashboard> Dashboards => _context.Set<Dashboard>();
    public DbSet<ViewDefinition> ViewDefinitions => _context.Set<ViewDefinition>();
    public DbSet<ChatRoom> ChatRooms => _context.Set<ChatRoom>();
    public DbSet<ChatRoomMember> ChatRoomMembers => _context.Set<ChatRoomMember>();
    public DbSet<ChatMessage> ChatMessages => _context.Set<ChatMessage>();
    public DbSet<Attachment> Attachments => _context.Set<Attachment>();
    public DbSet<AttachmentLink> AttachmentLinks => _context.Set<AttachmentLink>();
    public DbSet<TaskAssignment> TaskAssignments => _context.Set<TaskAssignment>();
    public DbSet<Widget> Widgets => _context.Set<Widget>();
    public DbSet<OutboxMessage> OutboxMessages => _context.Set<OutboxMessage>();
    public DbSet<PasswordResetToken> PasswordResetTokens => _context.Set<PasswordResetToken>();

    public DbSet<T> Set<T>() where T : class => _context.Set<T>();
    public bool HasActiveTransaction => _currentTransaction != null;
    public ChangeTracker ChangeTracker => _context.ChangeTracker;

    public IExecutionStrategy CreateExecutionStrategy() => _context.Database.CreateExecutionStrategy();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return _currentTransaction;
        _currentTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
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
        var domainEvents = _context.ChangeTracker.CollectDomainEvents();
        
        if (domainEvents.Any())
        {
            var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage(
                domainEvent.GetType().Name,
                System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                DateTimeOffset.UtcNow)).ToList();

            _context.Set<OutboxMessage>().AddRange(outboxMessages);
            _context.ChangeTracker.ClearDomainEvents();
        }

        var totalChanges = await _context.SaveChangesAsync(cancellationToken);

        // NOTE: Circular dependency prevented direct trigger here. 
        // The Background job worker should poll or be triggered via a separate service in Application layer.

        return totalChanges;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        return await Connection.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        return await Connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
    {
        return await Connection.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }
}
