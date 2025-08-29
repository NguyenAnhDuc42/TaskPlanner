using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class TagRepository : BaseRepository<Tag>, ITagRepository
{
    public TagRepository(TaskPlanDbContext context) : base(context) { }
}
