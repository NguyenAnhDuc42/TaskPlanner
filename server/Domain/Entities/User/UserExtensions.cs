using System.Linq;

namespace Domain.Entities;

public static class UserExtensions
{
    public static IQueryable<User> ByEmail(this IQueryable<User> query, string email) => 
        query.Where(user => user.Email == email);
}
