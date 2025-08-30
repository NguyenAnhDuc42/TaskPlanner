using System;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Infrastructure.Data.Repositories;

public class ProjectFolderRepository : BaseRepository<ProjectFolder>, IProjectFolderRepository
{
    public ProjectFolderRepository(TaskPlanDbContext context) : base(context)
    {
    }

}
