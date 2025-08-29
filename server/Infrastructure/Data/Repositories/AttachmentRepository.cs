using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class AttachmentRepository : BaseRepository<Attachment>, IAttachmentRepository
{
    public AttachmentRepository(TaskPlanDbContext context) : base(context) { }
}
