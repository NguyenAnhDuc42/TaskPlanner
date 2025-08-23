using Application.Common.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class ProjectTaskRepository : BaseRepository<ProjectTask>, IProjectTaskRepository
    {
        public ProjectTaskRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}