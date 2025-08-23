using Application.Common.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Repositories
{
    public class ProjectFolderRepository : BaseRepository<ProjectFolder>, IProjectFolderRepository
    {
        public ProjectFolderRepository(TaskPlanDbContext context) : base(context)
        {
        }
        // Implement any ProjectFolder specific methods here if needed
    }
}
