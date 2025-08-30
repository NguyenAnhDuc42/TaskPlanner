using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Repositories;

public class ProjectWorkspaceRepository : BaseRepository<ProjectWorkspace>, IProjectWorkspaceRepository
{
    public ProjectWorkspaceRepository(TaskPlanDbContext context) : base(context)
    {
    }
}
