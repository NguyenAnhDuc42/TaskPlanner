using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Repositories;

public class ProjectSpaceRepository : BaseRepository<ProjectSpace>, IProjectSpaceRepository
{
    public ProjectSpaceRepository(TaskPlanDbContext context) : base(context)
    {
    }
}
