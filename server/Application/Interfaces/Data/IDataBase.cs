using Microsoft.EntityFrameworkCore;
using System.Data;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities;
using Domain.Entities.Support;

namespace Application.Interfaces.Data;

public interface IDataBase : IUnitOfWork
{
    IDbConnection Connection { get; }
    
    DbSet<User> Users { get; }
    DbSet<ProjectWorkspace> Workspaces { get; }
    DbSet<ProjectSpace> Spaces { get; }
    DbSet<ProjectFolder> Folders { get; }
    DbSet<ProjectTask> Tasks { get; }
    DbSet<WorkspaceMember> Members { get; }
    DbSet<EntityAccess> Access { get; }
    DbSet<Status> Statuses { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Document> Documents { get; }
    DbSet<Dashboard> Dashboards { get; }
    DbSet<Workflow> Workflows { get; }
}
