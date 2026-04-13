using Microsoft.EntityFrameworkCore;
using System.Data;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.Support;

namespace Application.Interfaces.Data;

public interface IDataBase : IUnitOfWork
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
    DbSet<ChatRoom> ChatRooms { get; }
    DbSet<ChatRoomMember> ChatRoomMembers { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<EntityAssetLink> EntityAssetLinks { get; }
    DbSet<TaskAssignment> TaskAssignments { get; }
    DbSet<Widget> Widgets { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
}
