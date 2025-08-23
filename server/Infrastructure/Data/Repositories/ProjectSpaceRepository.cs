using Application.Common.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class ProjectSpaceRepository : BaseRepository<ProjectSpace>, IProjectSpaceRepository
    {
        public ProjectSpaceRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}