using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Repositories;

public class ProjectTaskRepository : BaseRepository<ProjectTask>, IProjectTaskRepository
{
    public ProjectTaskRepository(TaskPlanDbContext context) : base(context)
    {
    }
}
