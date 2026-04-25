using Microsoft.EntityFrameworkCore;
using System.Data;
using Domain.Entities;

namespace Application.Interfaces.Data;

public interface IDataBase 
{
    IDbConnection Connection { get; }
    
    DbSet<User> Users { get; }
    DbSet<Session> Sessions { get; }
    DbSet<ProjectWorkspace> Workspaces { get; }
    DbSet<ProjectSpace> Spaces { get; }
    DbSet<ProjectFolder> Folders { get; }
    DbSet<ProjectTask> Tasks { get; }
    DbSet<Workflow> Workflows { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<EntityAccess> Access { get; }
    DbSet<Status> Statuses { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Document> Documents { get; }
    DbSet<Dashboard> Dashboards { get; }
    DbSet<ViewDefinition> ViewDefinitions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<EntityAssetLink> EntityAssetLinks { get; }
    DbSet<TaskAssignment> TaskAssignments { get; }
    DbSet<Widget> Widgets { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
