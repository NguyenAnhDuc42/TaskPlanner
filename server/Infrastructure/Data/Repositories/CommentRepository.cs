using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class CommentRepository : BaseRepository<Comment>, ICommentRepository
{
    public CommentRepository(TaskPlanDbContext context) : base(context) { }
}
