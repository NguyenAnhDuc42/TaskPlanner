using Application.Common.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class ProjectWorkspaceRepository : BaseRepository<ProjectWorkspace>, IProjectWorkspaceRepository
    {
        public ProjectWorkspaceRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}