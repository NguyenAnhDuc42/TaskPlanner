using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(TaskPlanDbContext context) : base(context) { }
}
