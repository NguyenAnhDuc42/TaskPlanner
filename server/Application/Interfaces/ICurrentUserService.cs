using Domain.Entities;

namespace server.Application.Interfaces
{
    public interface ICurrentUserService
    {
        Guid CurrentUserId();
        User CurrentUser();
        User CurrentUserWithSession();
    }
}
