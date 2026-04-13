using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        Guid CurrentUserId();
        User CurrentUser();
        Task<User> CurrentUserAsync(CancellationToken cancellationToken = default);
    }
}
