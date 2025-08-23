using Application.Common.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class CommentRepository : BaseRepository<Comment>, ICommentRepository
    {
        public CommentRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}