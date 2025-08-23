using Application.Common.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Repositories
{
    public class ProjectListRepository : BaseRepository<ProjectList>, IProjectListRepository
    {
        public ProjectListRepository(TaskPlanDbContext context) : base(context)
        {
        }
        // Implement any ProjectList specific methods here if needed
    }
}
