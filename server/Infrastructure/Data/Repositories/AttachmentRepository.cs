using Application.Common.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class AttachmentRepository : BaseRepository<Attachment>, IAttachmentRepository
    {
        public AttachmentRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}